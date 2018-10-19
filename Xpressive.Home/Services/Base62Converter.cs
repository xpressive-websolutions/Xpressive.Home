using System;
using System.Text;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal sealed class Base62Converter : IBase62Converter
    {
        private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public string ToBase62(ulong number)
        {
            var result = "";

            while (number > 0)
            {
                var temp = number % 62;
                result = Alphabet[(int)temp] + result;
                number = number / 62;

            }

            return result;
        }

        public string ToBase62(byte[] array)
        {
            var result = new StringBuilder();
            var temp = new byte[8];

            for (var i = 0; i < array.Length; i += 8)
            {
                for (var j = 0; j < temp.Length; j++)
                {
                    temp[j] = 0;
                }

                var length = Math.Min(8, array.Length - i);
                Array.Copy(array, i, temp, 0, length);
                var uint64 = BitConverter.ToUInt64(temp, 0);
                result.Append(ToBase62(uint64));
            }

            return result.ToString();
        }
    }
}
