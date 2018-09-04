using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.WebApi.Controllers
{
    [Route("api/v1/radio")]
    public class RadioStationController : Controller
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
        public async Task<IEnumerable<object>> GetCategoriesAsync([FromQuery] string parentId = null)
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
        public async Task<object> SearchAsync([FromQuery] string query)
        {
            var result = await _radioStationService.SearchStationsAsync(query);

            return new
            {
                result.Stations,
                ShowMore = result.ShowMoreId
            };
        }

        [HttpGet, Route("station")]
        public async Task<object> GetStationsAsync([FromQuery] string categoryId)
        {
            var stations = await _radioStationService.GetStationsAsync(categoryId);

            return new
            {
                stations.Stations,
                ShowMore = stations.ShowMoreId
            };
        }

        [HttpGet, Route("playing")]
        public async Task<object> GetPlaying([FromQuery] string stationId)
        {
            return await _radioStationService.GetStationDetailAsync(stationId);
        }

        [HttpPost, Route("play")]
        public void Play([FromQuery] string deviceId)
        {
            _messageQueue.Publish(new CommandMessage("Sonos", deviceId, "Play", new Dictionary<string, string>()));
        }

        [HttpPost, Route("stop")]
        public void Stop([FromQuery] string deviceId)
        {
            _messageQueue.Publish(new CommandMessage("Sonos", deviceId, "Stop", new Dictionary<string, string>()));
        }

        [HttpPost, Route("play/radio")]
        public void PlayRadio([FromQuery] string deviceId, [FromBody] RadioStationDto radioStation)
        {
            var url = _radioStationService.GetStreamUrl(radioStation.Id);
            var parameters = new Dictionary<string, string>
            {
                {"Stream", url},
                {"Title", radioStation.Name}
            };
            _messageQueue.Publish(new CommandMessage("Sonos", deviceId, "Play Radio", parameters));
        }

        [HttpPost, Route("volume")]
        public void ChangeVolume([FromQuery] string deviceId, [FromQuery] int volume)
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
        public async Task<IActionResult> Unstar([FromBody] RadioStationDto dto)
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
