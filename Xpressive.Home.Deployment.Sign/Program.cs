using System;
using System.IO;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Digests;

namespace Xpressive.Home.Deployment.Sign
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1 || !File.Exists(args[0]))
            {
                return;
            }

            var file = args[0];
            var directory = Path.GetDirectoryName(file);

            var keys = LoadKeys(directory);
            var hash = GetHash(file);
            var signature = Sign(hash, keys.Item1);
            var signatureFile = file + ".sign";
            File.WriteAllText(signatureFile, Convert.ToBase64String(signature));
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

        private static byte[] Sign(byte[] hash, byte[] privateKey)
        {
            using (var key = CngKey.Import(privateKey, CngKeyBlobFormat.EccPrivateBlob))
            {
                using (var ecdsa = new ECDsaCng(key))
                {
                    return ecdsa.SignHash(hash);
                }
            }
        }

        private static Tuple<byte[], byte[]> LoadKeys(string directory)
        {
            var file = Path.Combine(directory, "keys.bin");
            if (File.Exists(file))
            {
                var lines = File.ReadAllLines(file);

                if (lines.Length >= 2)
                {
                    var privateKey = Convert.FromBase64String(lines[0]);
                    var publicKey = Convert.FromBase64String(lines[1]);
                    return Tuple.Create(privateKey, publicKey);
                }
            }

            return CreateKeys(file);
        }

        private static Tuple<byte[], byte[]> CreateKeys(string file)
        {
            var key = CngKey.Create(CngAlgorithm.ECDsaP521, null, new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextExport });
            var provider = new ECDsaCng(key);
            var publicKey = provider.Key.Export(CngKeyBlobFormat.EccPublicBlob);
            var privateKey = provider.Key.Export(CngKeyBlobFormat.EccPrivateBlob);

            File.WriteAllLines(file, new []
            {
                Convert.ToBase64String(privateKey),
                Convert.ToBase64String(publicKey)
            });

            return Tuple.Create(privateKey, publicKey);
        }
    }
}
