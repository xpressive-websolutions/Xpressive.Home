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
            await _service.DownloadNewestVersionAsync();
        }
    }
}
