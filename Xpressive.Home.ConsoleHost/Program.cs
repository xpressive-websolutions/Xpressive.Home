using System;

namespace Xpressive.Home.ConsoleHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Start Xpressive.Home");

            Setup.Run();

            Console.ReadLine();
        }
    }
}
