using System;
using log4net;

namespace Xpressive.Home.ConsoleHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var log = LogManager.GetLogger(typeof (Program));
            log.Info("Start Xpressive.Home");

            Setup.Run();

            Console.ReadLine();
        }
    }
}
