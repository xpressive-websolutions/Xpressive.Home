using System;
using System.Configuration;
using System.Reflection;
using DbUp;

namespace Xpressive.Home.DatabaseMigrator
{
    public static class DbMigrator
    {
        public static void Run()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"]?.ConnectionString;

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("ConnectionString must not be null or empty.");
            }

            var upgrader = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                throw result.Error;
            }
        }
    }
}
