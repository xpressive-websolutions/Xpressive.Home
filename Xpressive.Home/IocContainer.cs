using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using Autofac;

namespace Xpressive.Home
{
    internal static class IocContainer
    {
        private static IContainer _container;

        public static void Build(string connectionString)
        {
            var builder = new ContainerBuilder();
            _container = builder.Build();

            builder = new ContainerBuilder();
            builder.RegisterAssemblyModules(Assembly.Load("Xpressive.Home"));
            builder.RegisterAssemblyModules(Assembly.Load("Xpressive.Home.Services"));
            builder.RegisterAssemblyModules(Assembly.Load("Xpressive.Home.WebApi"));

            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            var plugins = Directory.GetFiles(directory, "Xpressive.Home.Plugins.*.dll", SearchOption.TopDirectoryOnly);

            foreach (var plugin in plugins)
            {
                var pluginFileName = Path.GetFileName(plugin);

                if (string.IsNullOrEmpty(pluginFileName))
                {
                    continue;
                }

                builder.RegisterAssemblyModules(Assembly.LoadFile(plugin));
            }

            builder.Register(_ =>
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                return (DbConnection)connection;
            });
            builder.Register(cc => _container);
            builder.Update(_container);
        }

        public static T Resolve<T>()
        {
            return _container.Resolve<T>();
        }

        public static void Dispose()
        {
            _container.Dispose();
        }
    }
}
