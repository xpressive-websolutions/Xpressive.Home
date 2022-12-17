using DbUp;
using System;
using System.Reflection;

namespace Xpressive.Home.DatabaseMigrator
{
    public static class DbMigrator
    {
        public static void Run(string connectionString)
        {
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
