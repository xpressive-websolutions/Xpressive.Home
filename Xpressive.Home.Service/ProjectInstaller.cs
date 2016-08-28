using System.ComponentModel;
using System.Configuration.Install;

namespace Xpressive.Home.Service
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}
