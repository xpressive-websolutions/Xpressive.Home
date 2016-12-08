using System;

namespace Xpressive.Home.Plugins.Tado
{
    internal class TokenDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public DateTime Expires { get; set; }
    }
}
