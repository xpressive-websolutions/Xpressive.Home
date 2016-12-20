using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpressive.Home.Plugins.Zwave
{
    internal static class ZwaveDeviceLibraryResolver
    {
        public static void Resolve(ZwaveDeviceLibrary library, ZwaveDevice device)
        {
            var devices = library.Devices
                .Where(d => d.ManufacturerId.Equals(device.ManufacturerId))
                .Where(d => d.ProductId.Equals(device.ProductId))
                .ToList();

            devices = Filter(devices, d => d.ProductTypeId.Equals(device.ProductType));
            devices = Filter(devices, d => d.OemVersion.Equals(device.Application, StringComparison.OrdinalIgnoreCase));
            devices = Filter(devices, d => d.FrequencyName.ToLowerInvariant().Contains("europe"));

            var libraryDevice = devices.FirstOrDefault();
            if (libraryDevice != null)
            {
                device.Manufacturer = libraryDevice.Brand;
                device.ProductName = libraryDevice.Name;
                device.ProductDescription = libraryDevice.Description;
                device.ImagePath = libraryDevice.Image;
            }
        }

        private static List<ZwaveDeviceLibraryItem> Filter(List<ZwaveDeviceLibraryItem> items, Func<ZwaveDeviceLibraryItem, bool> predicate)
        {
            if (items.Count <= 1)
            {
                return items;
            }

            var filtered = items.Where(predicate).ToList();

            if (filtered.Count > 0)
            {
                return filtered;
            }

            return items;
        }
    }
}
