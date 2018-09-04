using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Xpressive.Home.ConsoleHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Log.Logger = log;
            Log.Information("Start Xpressive.Home");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            var connectionString = configuration.GetConnectionString("ConnectionString");

            using (Setup.Run(connectionString))
            {
                Console.ReadLine();
                Log.Debug("Stopping Xpressive.Home");
            }

            Log.Information("Stopped Xpressive.Home");
        }
    }
}
