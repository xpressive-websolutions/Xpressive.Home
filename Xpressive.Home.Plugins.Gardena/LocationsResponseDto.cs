using System.Collections.Generic;

namespace Xpressive.Home.Plugins.Gardena
{
    internal class LocationsResponseDto
    {
        public List<LocationsResponseDtoLocation> Locations { get; set; }
    }

    internal class LocationsResponseDtoLocation
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Devices { get; set; }
    }
}
