namespace Xpressive.Home.Plugins.Zwave
{
    internal class ZwaveDeviceLibraryItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }
        public string Identifier { get; set; }
        public string CertificationNumber { get; set; }
        public string OemVersion { get; set; }
        public string HardwarePlatform { get; set; }
        public string ZWaveVersion { get; set; }
        public string LibraryType { get; set; }
        public string SpecificDeviceClass { get; set; }
        public string GenericDeviceClass { get; set; }
        public string DeviceType { get; set; }
        public int ManufacturerId { get; set; }
        public int ProductTypeId { get; set; }
        public int ProductId { get; set; }
        public string FrequencyName { get; set; }
        public string Image { get; set; }
    }
}
