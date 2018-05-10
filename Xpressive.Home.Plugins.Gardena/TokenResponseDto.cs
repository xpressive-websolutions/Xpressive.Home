namespace Xpressive.Home.Plugins.Gardena
{
    internal class TokenResponseDto
    {
        public TokenResponseDtoData Data { get; set; }
    }

    internal class TokenResponseDtoData
    {
        public TokenResponseDtoDataAttributes Attributes { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
    }

    internal class TokenResponseDtoDataAttributes
    {
        public int ExpiresIn { get; set; }
        public string Provider { get; set; }
        public string RefreshToken { get; set; }
        public string UserId { get; set; }
    }
}
