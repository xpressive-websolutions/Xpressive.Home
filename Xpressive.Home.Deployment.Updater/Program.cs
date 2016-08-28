using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;

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
            if (!package.Exists || !package.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var tmp = Unzip(package);
            StopService();
            Copy(tmp);
            StartService();
            Directory.Delete(tmp, true);
            package.Delete();
        }

        private static void Copy(string tempDirectory)
        {
            var target = Assembly.GetExecutingAssembly().Location;
            var files = new DirectoryInfo(tempDirectory).EnumerateFiles();

            Parallel.ForEach(files, f =>
            {
                File.Copy(f.FullName, Path.Combine(target, f.Name), true);
            });
        }

        private static void StartService()
        {
            using (var service = new ServiceController("Xpressive.Home.Service"))
            {
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
            }
        }

        private static void StopService()
        {
            using (var service = new ServiceController("Xpressive.Home.Service"))
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
            }
        }

        private static string Unzip(FileInfo package)
        {
            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(tmp);

            using (var ps = package.OpenRead())
            {
                using (var archive = new ZipArchive(ps))
                {
                    Parallel.ForEach(archive.Entries, e => Unzip(tmp, e));
                }
            }

            return tmp;
        }

        private static void Unzip(string tempDirectory, ZipArchiveEntry entry)
        {
            if (entry.Name.EndsWith(".exe.config"))
            {
                return;
            }

            if (entry.Name.Equals("Xpressive.Home.Deployment.Updater.exe", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            using (var es = File.Create(Path.Combine(tempDirectory, entry.Name)))
            {
                using (var ss = entry.Open())
                {
                    ss.CopyTo(es);
                }
            }
        }
    }
}
