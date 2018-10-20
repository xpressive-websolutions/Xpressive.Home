using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    /// <summary>
    /// Is using the OPML interface provided by tunein
    /// </summary>
    internal sealed class TuneInRadioStationService : ITuneInRadioStationService
    {
        private static readonly XmlSerializer _serializer = new XmlSerializer(typeof(OpmlDocument));
        private static readonly ConcurrentDictionary<string, Uri> _categories =
            new ConcurrentDictionary<string, Uri>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, Uri> _showMoreStations =
            new ConcurrentDictionary<string, Uri>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, Task<string>> _stationCallSigns =
            new ConcurrentDictionary<string, Task<string>>(StringComparer.Ordinal);

        private readonly IBase62Converter _base62Converter;
        private readonly IHttpClientProvider _httpClientProvider;

        public TuneInRadioStationService(IBase62Converter base62Converter, IHttpClientProvider httpClientProvider)
        {
            _base62Converter = base62Converter;
            _httpClientProvider = httpClientProvider;
        }

        public async Task<IEnumerable<TuneInRadioStationCategory>> GetCategoriesAsync(string parentId = null)
        {
            if (parentId == null)
            {
                return await GetCategoriesInternalAsync("http://air.radiotime.com/");
            }

            Uri url;
            if (_categories.TryGetValue(parentId, out url))
            {
                return await GetCategoriesInternalAsync(url.AbsoluteUri);
            }

            return Enumerable.Empty<TuneInRadioStationCategory>();
        }

        public async Task<TuneInRadioStations> GetStationsAsync(string categoryId)
        {
            if (string.IsNullOrEmpty(categoryId))
            {
                return new TuneInRadioStations();
            }

            Uri url;
            if (!_categories.TryGetValue(categoryId, out url) && !_showMoreStations.TryGetValue(categoryId, out url))
            {
                return new TuneInRadioStations();
            }

            return await GetStationsInternalAsync(url.AbsoluteUri);
        }

        public async Task<TuneInRadioStations> SearchStationsAsync(string query)
        {
            var url = $"http://opml.radiotime.com/Search.ashx?query={WebUtility.UrlDecode(query)}&types=station&name";
            return await GetStationsInternalAsync(url);
        }

        public string GetStreamUrl(string stationId)
        {
            return $"x-sonosapi-stream:{stationId}?sid=254&flags=32";
        }

        public async Task<TuneInRadioStationDetail> GetStationDetailAsync(string stationId)
        {
            var callSign = await _stationCallSigns.GetOrAdd(stationId, GetStationCallSignAsync);
            var url = $"http://opml.radiotime.com/Search.ashx?query={callSign}&call";
            var opml = await GetDocumentAsync(url);

            if (opml.Header.Status != 200)
            {
                return null;
            }

            var outline = opml.Body.Outlines?.FirstOrDefault(o => stationId.Equals(o.GuideId));

            if (outline == null)
            {
                return null;
            }

            return new TuneInRadioStationDetail
            {
                Id = outline.GuideId,
                Name = outline.Text,
                Playing = outline.Playing ?? outline.Subtext,
                PlayingImageUrl = outline.PlayingImage ?? outline.Image
            };
        }

        private async Task<string> GetStationCallSignAsync(string stationId)
        {
            var result = await _httpClientProvider.Get().GetStringAsync("http://opml.radiotime.com/Describe.ashx?id=" + stationId);
            var document = new XmlDocument();
            document.LoadXml(result);

            var status = document.SelectSingleNode("/opml/head/status")?.InnerText;

            if (string.IsNullOrEmpty(status) || status != "200")
            {
                return null;
            }

            var callSign = document.SelectSingleNode("/opml/body/outline/station/call_sign")?.InnerText;
            return Uri.EscapeDataString(callSign);
        }

        private async Task<IEnumerable<TuneInRadioStationCategory>> GetCategoriesInternalAsync(string url)
        {
            var document = await GetDocumentAsync(url);

            if (document == null || document.Header.Status != 200)
            {
                return Enumerable.Empty<TuneInRadioStationCategory>();
            }

            return ConvertOutlinesToCategories(document.Body.Outlines);
        }

        private async Task<TuneInRadioStations> GetStationsInternalAsync(string url)
        {
            var document = await GetDocumentAsync(url);

            if (document == null || document.Header.Status != 200)
            {
                return new TuneInRadioStations();
            }

            return ConvertOutlinesToStations(document.Body.Outlines);
        }

        private IList<TuneInRadioStationCategory> ConvertOutlinesToCategories(IEnumerable<OpmlOutline> outlines)
        {
            var result = new List<TuneInRadioStationCategory>();

            foreach (var outline in outlines.Where(o => o.Type == "link"))
            {
                if (outline.Outlines != null && "related".Equals(outline.Key))
                {
                    result.AddRange(ConvertOutlinesToCategories(outline.Outlines));
                }

                if ("link".Equals(outline.Type))
                {
                    var id = GetHash(outline.Url);
                    result.Add(new TuneInRadioStationCategory
                    {
                        Id = id,
                        Name = outline.Text
                    });

                    _categories.TryAdd(id, new Uri(outline.Url, UriKind.Absolute));
                }
            }

            return result;
        }

        private TuneInRadioStations ConvertOutlinesToStations(IEnumerable<OpmlOutline> outlines)
        {
            var result = new TuneInRadioStations();

            foreach (var outline in outlines)
            {
                if (outline.Outlines != null && ("stations".Equals(outline.Key) || "local".Equals(outline.Key)))
                {
                    var children = ConvertOutlinesToStations(outline.Outlines);
                    result.Stations.AddRange(children.Stations);

                    if (string.IsNullOrEmpty(result.ShowMoreId) && !string.IsNullOrEmpty(children.ShowMoreId))
                    {
                        result.ShowMoreId = children.ShowMoreId;
                    }
                }

                if ("nextStations".Equals(outline.Key) && "link".Equals(outline.Type))
                {
                    var hash = GetHash(outline.Url);
                    result.ShowMoreId = hash;
                    _showMoreStations.TryAdd(hash, new Uri(outline.Url));
                }

                if ("audio".Equals(outline.Type) && outline.Formats != null && outline.Formats.Contains("mp3"))
                {
                    result.Stations.Add(new Radio
                    {
                        Id = outline.GuideId,
                        Name = outline.Text,
                        ImageUrl = outline.Image
                    });
                }
            }

            return result;
        }

        private async Task<OpmlDocument> GetDocumentAsync(string url)
        {
            var stream = await _httpClientProvider.Get().GetStreamAsync(url);
            return _serializer.Deserialize(stream) as OpmlDocument;
        }

        private string GetHash(string url)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
                return _base62Converter.ToBase62(bytes);
            }
        }
    }

    [XmlRoot("opml")]
    public class OpmlDocument
    {
        [XmlElement("head")]
        public OpmlHeader Header { get; set; }

        [XmlElement("body")]
        public OpmlBody Body { get; set; }
    }

    public class OpmlHeader
    {
        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("status")]
        public int Status { get; set; }
    }

    public class OpmlBody
    {
        [XmlElement("outline")]
        public List<OpmlOutline> Outlines { get; set; }
    }

    public class OpmlOutline
    {
        [XmlAttribute("guide_id")]
        public string GuideId { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlAttribute("key")]
        public string Key { get; set; }

        [XmlAttribute("text")]
        public string Text { get; set; }

        [XmlAttribute("subtext")]
        public string Subtext { get; set; }

        [XmlAttribute("playing")]
        public string Playing { get; set; }

        [XmlAttribute("playing_image")]
        public string PlayingImage { get; set; }

        [XmlAttribute("now_playing_id")]
        public string NowPlayingId { get; set; }

        [XmlAttribute("image")]
        public string Image { get; set; }

        [XmlAttribute("URL")]
        public string Url { get; set; }

        [XmlAttribute("reliability")]
        public int Reliability { get; set; }

        [XmlAttribute("formats")]
        public string Formats { get; set; }

        [XmlAttribute("media_type")]
        public string MediaType { get; set; }

        [XmlElement("outline")]
        public List<OpmlOutline> Outlines { get; set; }
    }
}
