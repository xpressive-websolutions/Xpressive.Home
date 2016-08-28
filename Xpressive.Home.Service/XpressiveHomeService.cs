using System;
using System.ServiceProcess;
using log4net;

namespace Xpressive.Home.Service
{
    public partial class XpressiveHomeService : ServiceBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(XpressiveHomeService));
        private IDisposable _application;

        public XpressiveHomeService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _log.Info("Start Xpressive.Home");
            _application = Setup.Run();
        }

        protected override void OnStop()
        {
            _log.Info("Stopping Xpressive.Home");
            _application.Dispose();
            _log.Info("Stopped Xpressive.Home");
        }
    }
}
