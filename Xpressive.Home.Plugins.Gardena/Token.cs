using System;

namespace Xpressive.Home.Plugins.Gardena
{
    internal class Token
    {
        public string UserId { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expires { get; set; }
    }
}
