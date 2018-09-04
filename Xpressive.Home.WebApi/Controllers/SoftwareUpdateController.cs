using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.WebApi.Controllers
{
    [Route("api/v1/softwareupdate")]
    public class SoftwareUpdateController : Controller
    {
        private readonly ISoftwareUpdateDownloadService _service;

        public SoftwareUpdateController(ISoftwareUpdateDownloadService service)
        {
            _service = service;
        }

        [HttpGet, Route("hasNewVersion")]
        public bool IsUpdateAvailable()
        {
            return _service.IsNewVersionAvailable();
        }

        [HttpPost, Route("start")]
        public async Task Update()
        {
            var file = await _service.DownloadNewestVersionAsync();
            if (file == null)
            {
                return;
            }

            var location = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var updater = Path.Combine(location, "Xpressive.Home.Deployment.Updater.exe");
            Process.Start(updater, "\"" + file.FullName + "\"");
        }
    }
}
