using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using RestSharp;

namespace Xpressive.Home.ProofOfConcept.Gateways.TextToSpeech
{
    // http://www.voicerss.org/api/documentation.aspx
    internal class TextToSpeechGateway : GatewayBase
    {
        public TextToSpeechGateway() : base("Text to speech")
        {
            _actions.Add(new Action("Speak")
            {
                Fields =
                {
                    "Language",
                    "Text"
                }
            });

            _actions.Add(new Action("Say Time")
            {
                Fields =
                {
                    "Language"
                }
            });
        }

        protected override async Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            var language = values["Language"];
            var key = ((TextToSpeechDevice)device).ApiKey;

            switch (action.Name.ToLowerInvariant())
            {
                case "say time":
                    await ExecuteSayTime(key, language);
                    break;
                case "speak":
                    var text = values["Text"];
                    await ExecuteSpeak(key, language, text);
                    break;
            }
        }

        protected override Task<string> GetInternal(IDevice device, string property)
        {
            return Task.FromResult<string>(null);
        }

        private async Task ExecuteSpeak(string key, string language, string text)
        {
            var client = new RestClient("https://api.voicerss.org");
            var request = new RestRequest(Method.GET);
            request.AddQueryParameter("key", key);
            request.AddQueryParameter("hl", language);
            request.AddQueryParameter("c", "WAV");
            request.AddQueryParameter("src", text);

            var speech = await client.ExecuteTaskAsync<byte[]>(request);
            using (var stream = new MemoryStream(speech.RawBytes))
            {
                var player = new SoundPlayer(stream);
                player.Play();
            }
        }

        private async Task ExecuteSayTime(string key, string language)
        {
            var text = string.Format("Es ist {0} Uhr", System.DateTime.Now.Hour);

            if (System.DateTime.Now.Minute != 0)
            {
                text += " " + System.DateTime.Now.Minute;
            }

            await ExecuteSpeak(key, language, text);
        }
    }
}