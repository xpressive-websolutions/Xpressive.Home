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
                .Where(d => string.Equals(d.ManufacturerId, device.ManufacturerId, StringComparison.OrdinalIgnoreCase))
                .Where(d => string.Equals(d.BasicClass, device.BasicType.ToString("x2"), StringComparison.OrdinalIgnoreCase))
                .Where(d => string.Equals(d.GenericClass, device.GenericType.ToString("x2"), StringComparison.OrdinalIgnoreCase))
                .ToList();

            devices = Filter(devices, d => string.Equals(d.SpecificClass, device.SpecificType.ToString("x2"), StringComparison.OrdinalIgnoreCase));
            devices = Filter(devices, d => string.Equals(d.ProductType, device.ProductType, StringComparison.OrdinalIgnoreCase));
            devices = Filter(devices, d => string.Equals(d.RfFrequency, "EU", StringComparison.OrdinalIgnoreCase));

            var libraryDevice = devices.FirstOrDefault();
            if (libraryDevice != null)
            {
                device.Manufacturer = libraryDevice.BrandName;
                device.ProductName = libraryDevice.ProductName;
                device.ProductDescription = libraryDevice.Description.FirstOrDefault()?.Description;
                device.ImagePath = libraryDevice.DeviceImage;
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
