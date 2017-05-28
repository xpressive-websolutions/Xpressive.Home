using System;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace Xpressive.Home.Plugins.NissanLeaf
{
    internal sealed class BlowfishEncryptionService : IBlowfishEncryptionService
    {
        public string Encrypt(string plainText, string key)
        {
            var engine = new BlowfishEngine();
            var cipher = new PaddedBufferedBlockCipher(engine);
            var keyBytes = new KeyParameter(Encoding.UTF8.GetBytes(key));

            cipher.Init(true, keyBytes);

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var result = new byte[cipher.GetOutputSize(plainBytes.Length)];
            var length = cipher.ProcessBytes(plainBytes, 0, plainBytes.Length, result, 0);

            cipher.DoFinal(result, length);

            var base64 = Convert.ToBase64String(result);
            return base64;
        }
    }
}
