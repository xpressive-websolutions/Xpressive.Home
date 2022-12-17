using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Octokit;
using Org.BouncyCastle.Crypto.Digests;
using Polly;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal sealed class SoftwareUpdateDownloadService : ISoftwareUpdateDownloadService, IStartable, IDisposable
    {
        private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private bool _isUpdateAvailable;

        public bool IsNewVersionAvailable()
        {
            return _isUpdateAvailable;
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

        public void Start()
        {
            Task.Run(async () =>
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    await CheckForUpdateAsync();
                    await Task.Delay(TimeSpan.FromHours(1), _cancellationToken.Token).ContinueWith(_ => { });
                }
            });
        }

        public void Dispose()
        {
            _cancellationToken.Cancel();
            _cancellationToken.Dispose();
        }

        private async Task CheckForUpdateAsync()
        {
            var policy = Policy
                .Handle<RateLimitExceededException>()
                .WaitAndRetryAsync(5, retry => TimeSpan.FromSeconds(Math.Pow(2, retry)));

            await policy.ExecuteAsync(async () =>
            {
                var release = await GetNewestReleaseAsync();

                if (release == null)
                {
                    _isUpdateAvailable = false;
                    return;
                }

                var newestVersion = release.Name.TrimStart('v');
                var version = GetCurrentVersion();

                _isUpdateAvailable = !string.Equals(newestVersion, version);
            });
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
            // TODO
            return true;
            //const string publicKey =
            //    "RUNTNUIAAAABLcbMBFKtpFlBY0j6TnD5DTeDjy2UoTt3ik2yhY9RGB8AdkZ44DTLG3L3e8S7g+bRmhwygtlsGUFEUMGiJ2SBeGAABS5nBJRfQhAA+vmishz0EWYXD5FUYClQaRHlrH9clkB9pDakzPSvPbGnpMuHgCWa6LniAb1zExIPbYv9zHlfqOA=";

            //using (var key = CngKey.Import(Convert.FromBase64String(publicKey), CngKeyBlobFormat.EccPublicBlob))
            //{
            //    using (var ecdsa = new ECDsaCng(key))
            //    {
            //        return ecdsa.VerifyHash(hash, signature);
            //    }
            //}
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

        private async Task<Release> GetNewestReleaseAsync()
        {
            var client = new GitHubClient(new ProductHeaderValue("Xpressive.Home", GetCurrentVersion()));
            var releases = await client.Repository.Release.GetAll("xpressive-websolutions", "Xpressive.Home");

            return releases
                .Where(r => !r.Draft)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();
        }

        private string GetCurrentVersion()
        {
            var attribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var version = attribute.InformationalVersion.TrimStart('v');
            return version;
        }
    }
}
