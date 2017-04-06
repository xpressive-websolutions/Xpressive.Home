namespace Xpressive.Home.Contracts.Services
{
    public interface IBase62Converter
    {
        string ToBase62(ulong number);

        string ToBase62(byte[] array);
    }
}
