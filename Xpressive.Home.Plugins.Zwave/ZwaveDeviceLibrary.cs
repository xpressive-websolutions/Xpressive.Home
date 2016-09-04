using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using log4net;

namespace Xpressive.Home.Plugins.Zwave
{
    internal class ZwaveDeviceLibrary
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ZwaveDeviceLibrary));
        private List<ZwaveDeviceLibraryItem> _devices;

        public ZwaveDeviceLibrary()
        {
            _devices = new List<ZwaveDeviceLibraryItem>();
        }

        public IEnumerable<ZwaveDeviceLibraryItem> Devices => _devices.AsReadOnly();

        public async Task Load()
        {
            var stream = await DownloadZipFile();
            var files = GetXmlFiles(stream);
            _devices = LoadIntoLibrary(files);
        }

        private static async Task<Stream> DownloadZipFile()
        {
            const string url = "http://www.pepper1.net/zwavedb/device/export/device_archive.zip";

            using (var client = new WebClient())
            {
                var binary = await client.DownloadDataTaskAsync(url);
                return new MemoryStream(binary);
            }
        }

        private static IEnumerable<Tuple<string, Stream>> GetXmlFiles(Stream stream)
        {
            var archive = new System.IO.Compression.ZipArchive(stream);

            foreach (var zipArchiveEntry in archive.Entries)
            {
                yield return Tuple.Create(zipArchiveEntry.Name, zipArchiveEntry.Open());
            }
        }

        private static List<ZwaveDeviceLibraryItem> LoadIntoLibrary(IEnumerable<Tuple<string, Stream>> streams)
        {
            var devices = new List<ZwaveDeviceLibraryItem>();

            foreach (var stream in streams)
            {
                if (!stream.Item1.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    var reader = new XmlTextReader(stream.Item2)
                    {
                        Namespaces = false,
                    };

                    var document = new XmlDocument();
                    document.Load(reader);

                    var device = LoadIntoLibrary(document);
                    devices.Add(device);
                }
                catch (XmlException e)
                {
                    _log.Error(stream.Item1 + ": " + e.Message);
                }
                catch (Exception e)
                {
                    _log.Error(stream.Item1 + ": " + e.Message, e);
                }
            }

            return devices;
        }

        private static ZwaveDeviceLibraryItem LoadIntoLibrary(XmlDocument document)
        {
            var device = new ZwaveDeviceLibraryItem();

            GetDeviceData(document, device);
            GetDescription(document, device);

            return device;
        }

        private static void GetDeviceData(XmlDocument document, ZwaveDeviceLibraryItem device)
        {
            device.ManufacturerId = document.SelectSingleNode("//deviceData/manufacturerId/@value")?.Value;
            device.ProductType = document.SelectSingleNode("//deviceData/productType/@value")?.Value;
            device.ProductId = document.SelectSingleNode("//deviceData/productId/@value")?.Value;
            device.LibraryType = document.SelectSingleNode("//deviceData/libType/@value")?.Value;
            device.ProtocolVersion = document.SelectSingleNode("//deviceData/protoVersion/@value")?.Value;
            device.ProtocolSubVersion= document.SelectSingleNode("//deviceData/protoSubVersion/@value")?.Value;
            device.ApplicationVersion= document.SelectSingleNode("//deviceData/appVersion/@value")?.Value;
            device.ApplicationSubVersion = document.SelectSingleNode("//deviceData/appSubVersion/@value")?.Value;
            device.BasicClass = document.SelectSingleNode("//deviceData/basicClass/@value")?.Value;
            device.GenericClass = document.SelectSingleNode("//deviceData/genericClass/@value")?.Value;
            device.SpecificClass = document.SelectSingleNode("//deviceData/specificClass/@value")?.Value;
            device.BeamSensor = document.SelectSingleNode("//deviceData/beamSensor")?.InnerText;
            device.RfFrequency = document.SelectSingleNode("//deviceData/rfFrequency")?.InnerText;

            var optional = document.SelectSingleNode("//deviceData/optional/@value")?.Value ?? "true";
            var listening = document.SelectSingleNode("//deviceData/listening/@value")?.Value ?? "false";
            var routing = document.SelectSingleNode("//deviceData/routing/@value")?.Value ?? "false";

            device.IsOptional = bool.Parse(optional);
            device.IsListening = bool.Parse(listening);
            device.IsRouting = bool.Parse(routing);
        }

        private static void GetDescription(XmlDocument document, ZwaveDeviceLibraryItem device)
        {
            device.Description = GetDescription(document, "description").ToList();
            device.WakeupNote = GetDescription(document, "wakeupNote").ToList();
            device.InclusionNote = GetDescription(document, "inclusionNote").ToList();

            device.DeviceImage = document.SelectSingleNode("//resourceLinks/deviceImage/@url")?.Value;
            device.ProductName = document.SelectSingleNode("//deviceDescription/productName")?.InnerText;
            device.ProductCode = document.SelectSingleNode("//deviceDescription/productCode")?.InnerText;
            device.BrandName = document.SelectSingleNode("//deviceDescription/brandName")?.InnerText;
        }

        private static IEnumerable<ZwaveDeviceLibraryItemDescription> GetDescription(XmlDocument document, string nodeName)
        {
            var nodeList = document.SelectNodes($"//deviceDescription/{nodeName}/lang");

            if (nodeList == null)
            {
                yield break;
            }

            var nodes = nodeList.OfType<XmlNode>().ToList();

            foreach (var node in nodes)
            {
                var language = node.Attributes?["xml:lang"]?.Value;

                if (language == null)
                {
                    continue;
                }

                yield return new ZwaveDeviceLibraryItemDescription
                {
                    Description = node.InnerText,
                    Language = language
                };
            }
        }
    }
}
