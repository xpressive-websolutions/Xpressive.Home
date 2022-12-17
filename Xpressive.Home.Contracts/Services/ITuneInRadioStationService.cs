using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Services
{
    public interface ITuneInRadioStationService
    {
        Task<IEnumerable<TuneInRadioStationCategory>> GetCategoriesAsync(string parentId = null);
        Task<TuneInRadioStationDetail> GetStationDetailAsync(string stationId);
        Task<TuneInRadioStations> GetStationsAsync(string categoryId);
        string GetStreamUrl(string stationId);
        Task<TuneInRadioStations> SearchStationsAsync(string query);
    }

    public class TuneInRadioStationCategory
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class TuneInRadioStations
    {
        public TuneInRadioStations()
        {
            Stations = new List<Radio>();
        }

        public List<Radio> Stations { get; set; }
        public string ShowMoreId { get; set; }
    }

    public class TuneInRadioStationDetail
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Playing { get; set; }
        public string PlayingImageUrl { get; set; }
    }
}
