using System.Collections.Generic;
using Newtonsoft.Json;

namespace Xpressive.Home.Plugins.ForeignExchangeRates
{
    public class FixerDto
    {
        [JsonProperty("base")]
        public string Base { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("rates")]
        public Dictionary<string, double> Rates { get; set; }
    }
}
