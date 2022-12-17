namespace Xpressive.Home.Plugins.Gardena
{
    internal class TokenRequestDto
    {
        public TokenRequestDto(string username, string password)
        {
            Data.Attributes.Username = username;
            Data.Attributes.Password = password;
        }

        public TokenRequestDtoData Data = new TokenRequestDtoData();
    }

    internal class TokenRequestDtoData
    {
        public TokenRequestDtoDataAttributes Attributes = new TokenRequestDtoDataAttributes();
        public string Type { get; set; } = "token";
    }

    internal class TokenRequestDtoDataAttributes
    {
        public string Password { get; set; }
        public string Username { get; set; }
    }
}
