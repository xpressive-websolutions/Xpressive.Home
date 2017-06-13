namespace Xpressive.Home.Plugins.NissanLeaf
{
    internal interface IBlowfishEncryptionService
    {
        string Encrypt(string plainText, string key);
    }
}
