namespace Xpressive.Home.ProofOfConcept.Gateways.TextToSpeech
{
    internal class TextToSpeechDevice : DeviceBase
    {
        private readonly string _apiKey;

        public TextToSpeechDevice(string apiKey) : base("TTS", "TTS")
        {
            _apiKey = apiKey;
        }

        public string ApiKey => _apiKey;
    }
}