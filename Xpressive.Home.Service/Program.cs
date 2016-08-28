using System.ServiceProcess;

namespace Xpressive.Home.Service
{
    public static class Program
    {
        public static void Main()
        {
            var servicesToRun = new ServiceBase[]
            {
                new XpressiveHomeService()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
