using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using log4net;

namespace Xpressive.Home.Plugins.Zwave
{
    internal class ZwaveDeviceLibrary
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ZwaveDeviceLibrary));
        private static readonly WebClient _webClient = new WebClient();
        private List<ZwaveDeviceLibraryItem> _devices;

        public ZwaveDeviceLibrary()
        {
            _devices = new List<ZwaveDeviceLibraryItem>();
        }

        public IEnumerable<ZwaveDeviceLibraryItem> Devices => _devices.AsReadOnly();

        public async Task Load(CancellationToken cancellationToken)
        {
            var directory = GetDirectory();
            CleanUp(directory);
            await DownloadDeviceDefinitions(directory, cancellationToken);
            _devices = GetItems(directory).ToList();
        }

        internal void CleanUp(string directory)
        {
            Directory.CreateDirectory(directory);

            var emptyFiles = Directory
                .GetFiles(directory, "*.xml", SearchOption.TopDirectoryOnly)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => int.Parse(f.Name.Substring(0, f.Name.IndexOf(".", StringComparison.OrdinalIgnoreCase))))
                .TakeWhile(f => f.Length == 0)
                .ToList();
            emptyFiles.ForEach(f => f.Delete());
        }

        internal async Task DownloadDeviceDefinitions(string directory, CancellationToken cancellationToken)
        {
            var xmlId = 1;
            var numberOfErrors = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var xmlPath = Path.Combine(directory, $"{xmlId}.xml");

                if (File.Exists(xmlPath))
                {
                    xmlId++;
                    continue;
                }

                var file = await DownloadDeviceDefinition(xmlId, cancellationToken);
                File.WriteAllBytes(xmlPath, file);

                if (file.Length == 0)
                {
                    numberOfErrors++;

                    if (numberOfErrors >= 20)
                    {
                        return;
                    }
                }
                else
                {
                    numberOfErrors = 0;
                }

                await Task.Delay(TimeSpan.FromSeconds(.1), cancellationToken).ContinueWith(t => { });
                xmlId++;
            }
        }

        internal async Task<byte[]> DownloadDeviceDefinition(int xmlId, CancellationToken cancellationToken)
        {
            try
            {
                var url = $"http://products.z-wavealliance.org/Products/{xmlId}/XML";
                var binary = await _webClient.DownloadDataTaskAsync(url);
                return binary;
            }
            catch (WebException)
            {
                return new byte[0];
            }
        }

        internal IEnumerable<ZwaveDeviceLibraryItem> GetItems(string directory)
        {
            var files = Directory
                .GetFiles(directory, "*.xml", SearchOption.TopDirectoryOnly)
                .Select(f => new FileInfo(f))
                .Where(f => f.Length > 0)
                .ToList();

            foreach (var file in files)
            {
                using (var fileStream = file.OpenRead())
                {
                    ZwaveDeviceLibraryItem device;
                    var document = new XmlDocument();

                    try
                    {
                        var reader = new XmlTextReader(fileStream)
                        {
                            Namespaces = false,
                        };

                        document.Load(reader);

                        device = LoadIntoLibrary(document);
                    }
                    catch (XmlException e)
                    {
                        _log.Error(file.Name + ": " + e.Message);
                        continue;
                    }
                    catch (Exception e)
                    {
                        _log.Error(file.Name + ": " + e.Message, e);
                        continue;
                    }

                    yield return device;
                }
            }
        }

        internal string GetDirectory()
        {
            return Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                "Data",
                "Zwave",
                "DeviceInformation");
        }

        private static ZwaveDeviceLibraryItem LoadIntoLibrary(XmlDocument document)
        {
            var device = new ZwaveDeviceLibraryItem();

            GetDeviceData(document, device);
            //GetDescription(document, device);

            return device;
        }

        private static void GetDeviceData(XmlDocument document, ZwaveDeviceLibraryItem device)
        {
            device.Id = int.Parse(GetNodeValue(document, "//ProductExport/Id"));
            device.Name = GetNodeValue(document, "//ProductExport/Name");
            device.Description = GetNodeValue(document, "//ProductExport/Description");
            device.Brand = GetNodeValue(document, "//ProductExport/Brand");
            device.Identifier = GetNodeValue(document, "//ProductExport/Identifier");
            device.CertificationNumber = GetNodeValue(document, "//ProductExport/CertificationNumber");
            device.OemVersion = GetNodeValue(document, "//ProductExport/OemVersion");
            device.HardwarePlatform = GetNodeValue(document, "//ProductExport/HardwarePlatform");
            device.ZWaveVersion = GetNodeValue(document, "//ProductExport/ZWaveVersion");
            device.LibraryType = GetNodeValue(document, "//ProductExport/LibraryType");
            device.SpecificDeviceClass = GetNodeValue(document, "//ProductExport/SpecificDeviceClass");
            device.GenericDeviceClass = GetNodeValue(document, "//ProductExport/GenericDeviceClass");
            device.DeviceType = GetNodeValue(document, "//ProductExport/DeviceType");
            device.ManufacturerId = GetNodeValueAsInt32(document, "//ProductExport/ManufacturerId");
            device.ProductTypeId = GetNodeValueAsInt32(document, "//ProductExport/ProductTypeId");
            device.ProductId = GetNodeValueAsInt32(document, "//ProductExport/ProductId");
            device.FrequencyName = GetNodeValue(document, "//ProductExport/FrequencyName");
            device.Image = GetNodeValue(document, "//ProductExport/Image");
        }

        private static string GetNodeValue(XmlDocument document, string xpath)
        {
            return document.SelectSingleNode(xpath)?.InnerText ?? string.Empty;
        }

        private static int GetNodeValueAsInt32(XmlDocument document, string xpath)
        {
            var value = document.SelectSingleNode(xpath)?.InnerText;

            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            if (value.IndexOf("(", StringComparison.Ordinal) > 0)
            {
                value = value.Substring(0, value.IndexOf("(", StringComparison.Ordinal));
            }

            if (value.StartsWith("0x00 0x", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(5).Trim();
            }

            if (value.StartsWith("0x0x", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(2);
            }

            if (value.Length <= 2)
            {
                return 0;
            }

            return Convert.ToInt32(value, 16);
        }
    }
}
