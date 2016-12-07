using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace Xpressive.Home.Deployment.Updater
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args == null || args.Length != 1 || string.IsNullOrEmpty(args[0]))
            {
                return;
            }

            var package = new FileInfo(args[0]);
            if (!package.Exists)
            {
                return;
            }

            var tmp = Unzip(package);
            StopService();
            Copy(tmp, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            StartService();
            Directory.Delete(tmp, true);
            package.Delete();
        }

        private static void Copy(string sourceDirectory, string targetDirectory)
        {
            var files = new DirectoryInfo(sourceDirectory).EnumerateFiles();

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            Parallel.ForEach(files, f =>
            {
                if (f.Name.EndsWith(".exe.config"))
                {
                    return;
                }

                if (f.Name.StartsWith("Xpressive.Home.Deployment.Updater.", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                File.Copy(f.FullName, Path.Combine(targetDirectory, f.Name), true);
            });

            var directories = Directory.GetDirectories(sourceDirectory);

            foreach (var directory in directories)
            {
                var name = new DirectoryInfo(directory).Name;
                var source = directory;
                var target = Path.Combine(targetDirectory, name);
                Copy(source, target);
            }
        }

        private static void StartService()
        {
            using (var service = new ServiceController("Xpressive.Home.Service"))
            {
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            }
        }

        private static void StopService()
        {
            using (var service = new ServiceController("Xpressive.Home.Service"))
            {
                if (service.Status == ServiceControllerStatus.Running)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
            }

            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        private static string Unzip(FileInfo package)
        {
            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(tmp);

            var fastZip = new FastZip();
            fastZip.ExtractZip(package.FullName, tmp, null);

            return tmp;
        }
    }
}
