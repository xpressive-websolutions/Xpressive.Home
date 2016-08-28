using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Digests;
using RestSharp;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal sealed class SoftwareUpdateDownloadService : ISoftwareUpdateDownloadService
    {
        private static readonly object _lock = new object();
        private static DateTime _lastCheck = DateTime.MinValue;
        private static bool _lastResult;

        public async Task<bool> IsNewVersionAvailableAsync()
        {
            lock (_lock)
            {
                if ((DateTime.UtcNow - _lastCheck) < TimeSpan.FromHours(3))
                {
                    return _lastResult;
                }
                _lastCheck = DateTime.UtcNow;
            }

            var attribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var newestRelease = await GetNewestReleaseAsync();

            if (newestRelease == null)
            {
                _lastResult = false;
                return false;
            }

            var newestVersion = newestRelease.Name.TrimStart('v');
            var version = attribute.InformationalVersion.TrimStart('v');

            _lastResult = !string.Equals(newestVersion, version);
            return _lastResult;
        }

        public async Task<FileInfo> DownloadNewestVersionAsync()
        {
            var package = await DownloadNewestReleaseAsync();
            var unzipped = await UnzipAsync(package);
            var release = unzipped.Item1;
            var signature = unzipped.Item2;

            var hash = GetHash(release.FullName);
            var sign = File.ReadAllBytes(signature.FullName);
            var isValid = ValidateHash(hash, sign);

            package.Delete();
            signature.Delete();

            if (isValid)
            {
                return release;
            }

            release.Delete();
            return null;
        }

        private static byte[] GetHash(string filePath)
        {
            var data = File.ReadAllBytes(filePath);

            var digest = new SkeinDigest(512, 512);
            digest.BlockUpdate(data, 0, data.Length);

            var hash = new byte[digest.GetDigestSize()];
            digest.DoFinal(hash, 0);
            return hash;
        }

        private bool ValidateHash(byte[] hash, byte[] signature)
        {
            const string publicKey = "RUNTNUIAAAABLcbMBFKtpFlBY0j6TnD5DTeDjy2UoTt3ik2yhY9RGB8AdkZ44DTLG3L3e8S7g+bRmhwygtlsGUFEUMGiJ2SBeGAABS5nBJRfQhAA+vmishz0EWYXD5FUYClQaRHlrH9clkB9pDakzPSvPbGnpMuHgCWa6LniAb1zExIPbYv9zHlfqOA=";

            using (var key = CngKey.Import(Convert.FromBase64String(publicKey), CngKeyBlobFormat.EccPublicBlob))
            {
                using (var ecdsa = new ECDsaCng(key))
                {
                    return ecdsa.VerifyHash(hash, signature);
                }
            }
        }

        private async Task<Tuple<FileInfo, FileInfo>> UnzipAsync(FileInfo package)
        {
            var signature = new FileInfo(Path.GetTempFileName());
            var release = new FileInfo(Path.GetTempFileName());

            using (var packageStream = package.OpenRead())
            {
                using (var packageArchive = new ZipArchive(packageStream, ZipArchiveMode.Read))
                {
                    foreach (var entry in packageArchive.Entries)
                    {
                        if (entry.Name.EndsWith(".zip.sign"))
                        {
                            using (var s = entry.Open())
                            {
                                using (var f = signature.OpenWrite())
                                {
                                    await s.CopyToAsync(f);
                                }
                            }
                        }
                        else if (entry.Name.EndsWith(".zip"))
                        {
                            using (var s = entry.Open())
                            {
                                using (var f = release.OpenWrite())
                                {
                                    await s.CopyToAsync(f);
                                }
                            }
                        }
                    }
                }
            }

            return Tuple.Create(release, signature);
        }

        private async Task<FileInfo> DownloadNewestReleaseAsync()
        {
            var release = await GetNewestReleaseAsync();

            using (var client = new HttpClient())
            {
                var data = await client.GetByteArrayAsync(release.Assets.Single().BrowserDownloadUrl);
                var path = Path.GetTempFileName();
                File.WriteAllBytes(path, data);
                return new FileInfo(path);
            }
        }

        private async Task<GithubReleaseDto> GetNewestReleaseAsync()
        {
            var releases = await GetReleasesAsync();
            return releases
                .Where(r => !r.Draft)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();
        }

        private async Task<IEnumerable<GithubReleaseDto>> GetReleasesAsync()
        {
            var client = new RestClient("https://api.github.com");
            var request = new RestRequest("/repos/xpressive-websolutions/Xpressive.Home/releases", Method.GET);
            request.AddHeader("Accept", "application/vnd.github.v3+json");
            var response = await client.ExecuteGetTaskAsync<List<GithubReleaseDto>>(request);

            if (response?.Data == null)
            {
                return Enumerable.Empty<GithubReleaseDto>();
            }

            return response.Data;
        }

        public class GithubReleaseDto
        {
            public string Name { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime PublishedAt { get; set; }
            public bool Draft { get; set; }
            public List<GithubReleaseAssetDto> Assets { get; set; }
        }

        public class GithubReleaseAssetDto
        {
            public string BrowserDownloadUrl { get; set; }
        }
    }
}
