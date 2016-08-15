using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;

namespace Xpressive.Home.Plugins.Lifx
{
    internal sealed class LifxHttpClient
    {
        private const string BaseUrl = "https://api.lifx.com";
        private readonly string _token;

        public LifxHttpClient(string token)
        {
            _token = token;
        }

        public async Task<IEnumerable<Light>> GetLights()
        {
            var client = new RestClient(BaseUrl);
            var request = new RestRequest("v1/lights/all");
            request.AddHeader("Authorization", $"Bearer {_token}");

            var result = await client.GetTaskAsync<List<Light>>(request);
            return result;
        }

        public async Task SwitchOn(Light light, int durationInSeconds = 0)
        {
            await ChangeState(light, new {power = "on", duration = durationInSeconds});
        }

        public async Task SwitchOff(Light light, int durationInSeconds = 0)
        {
            await ChangeState(light, new {power = "off", duration = durationInSeconds});
        }

        public async Task ChangeBrightness(Light light, double brightness, int durationInSeconds)
        {
            await ChangeState(light, new {power = "on", brightness = brightness, duration = durationInSeconds});
        }

        public async Task ChangeColor(Light light, string hexColor, int durationInSeconds)
        {
            await ChangeState(light, new {power = "on", color = hexColor, duration = durationInSeconds});
        }

        private async Task ChangeState(Light light, object payload)
        {
            var client = new RestClient(BaseUrl);
            var request = new RestRequest($"v1/lights/{light.Id}/state");
            request.AddHeader("Authorization", $"Bearer {_token}");
            request.AddJsonBody(payload);
            await client.PutTaskAsync<object>(request);
        }
    }
}
