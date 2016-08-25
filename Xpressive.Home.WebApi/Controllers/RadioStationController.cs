using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.WebApi.Controllers
{
    [RoutePrefix("api/v1/radio")]
    public class RadioStationController : ApiController
    {
        private readonly ITuneInRadioStationService _radioStationService;
        private readonly IFavoriteRadioStationService _favoriteRadioStationService;
        private readonly IMessageQueue _messageQueue;

        public RadioStationController(
            ITuneInRadioStationService radioStationService,
            IFavoriteRadioStationService favoriteRadioStationService,
            IMessageQueue messageQueue)
        {
            _radioStationService = radioStationService;
            _favoriteRadioStationService = favoriteRadioStationService;
            _messageQueue = messageQueue;
        }

        [HttpGet, Route("category")]
        public async Task<IEnumerable<object>> GetCategoriesAsync([FromUri] string parentId = null)
        {
            var categories = await _radioStationService.GetCategoriesAsync(parentId);
            var dtos = categories.Select(c => new
            {
                c.Id,
                c.Name
            });
            return dtos;
        }

        [HttpGet, Route("search")]
        public async Task<object> SearchAsync([FromUri] string query)
        {
            var result = await _radioStationService.SearchStationsAsync(query);

            return new
            {
                result.Stations,
                ShowMore = result.ShowMoreId
            };
        }

        [HttpGet, Route("station")]
        public async Task<object> GetStationsAsync([FromUri] string categoryId)
        {
            var stations = await _radioStationService.GetStationsAsync(categoryId);

            return new
            {
                stations.Stations,
                ShowMore = stations.ShowMoreId
            };
        }

        [HttpGet, Route("playing")]
        public async Task<object> GetPlaying([FromUri] string stationId)
        {
            return await _radioStationService.GetStationDetailAsync(stationId);
        }

        [HttpPost, Route("play")]
        public void Play([FromUri] string deviceId)
        {
            _messageQueue.Publish(new CommandMessage("Sonos", deviceId, "Play", new Dictionary<string, string>()));
        }

        [HttpPost, Route("stop")]
        public void Stop([FromUri] string deviceId)
        {
            _messageQueue.Publish(new CommandMessage("Sonos", deviceId, "Stop", new Dictionary<string, string>()));
        }

        [HttpPost, Route("play/radio")]
        public async Task PlayRadio([FromUri] string deviceId, [FromBody] RadioStationDto radioStation)
        {
            var url = await _radioStationService.GetStreamUrlAsync(radioStation.Id);
            var parameters = new Dictionary<string, string>
            {
                {"Stream", url},
                {"Title", radioStation.Name}
            };
            _messageQueue.Publish(new CommandMessage("Sonos", deviceId, "Play Radio", parameters));
        }

        [HttpPost, Route("volume")]
        public void ChangeVolume([FromUri] string deviceId, [FromUri] int volume)
        {
            var v = volume/100d;
            var parameters=new Dictionary<string, string>
            {
                {"Volume", v.ToString("F2")}
            };
            _messageQueue.Publish(new CommandMessage("Sonos", deviceId, "Change Volume", parameters));
        }

        [HttpGet, Route("starred")]
        public async Task<IEnumerable<FavoriteRadioStation>> GetFavorites()
        {
            var favorites = await _favoriteRadioStationService.GetAsync();
            return favorites;
        }

        [HttpPut, Route("star")]
        public async Task Star([FromBody] RadioStationDto dto)
        {
            var radioStation = new TuneInRadioStation
            {
                Id = dto.Id,
                Name = dto.Name,
                ImageUrl = dto.ImageUrl
            };

            await _favoriteRadioStationService.AddAsync(radioStation);
        }

        [HttpPut, Route("unstar")]
        public async Task<IHttpActionResult> Unstar([FromBody] RadioStationDto dto)
        {
            var favorites = await _favoriteRadioStationService.GetAsync();
            var favorite = favorites.SingleOrDefault(f => f.Id.Equals(dto.Id, StringComparison.Ordinal));

            if (favorite == null)
            {
                return BadRequest();
            }

            await _favoriteRadioStationService.RemoveAsync(favorite);
            return Ok();
        }

        public class RadioStationDto
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string ImageUrl { get; set; }
        }
    }
}
