using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Http;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.WebApi.Controllers
{
    [RoutePrefix("api/v1/softwareupdate")]
    public class SoftwareUpdateController : ApiController
    {
        private readonly ISoftwareUpdateDownloadService _service;

        public SoftwareUpdateController(ISoftwareUpdateDownloadService service)
        {
            _service = service;
        }

        [HttpGet, Route("hasNewVersion")]
        public async Task<bool> IsUpdateAvailable()
        {
            return await _service.IsNewVersionAvailableAsync();
        }

        [HttpPost, Route("start")]
        public async Task Update()
        {
            var file = await _service.DownloadNewestVersionAsync();
            if (file == null)
            {
                return;
            }

            Process.Start("Xpressive.Home.Deployment.Updater.exe", "\"" + file.FullName + "\"");
        }
    }
}
