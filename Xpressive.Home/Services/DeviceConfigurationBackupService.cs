using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal class DeviceConfigurationBackupService : IDeviceConfigurationBackupService
    {
        public DeviceConfigurationBackupService()
        {
            CreateDirectoryIfNotExisting();
        }

        public void Save<T>(string gatewayName, T deviceConfigurationBackup)
        {
            var path = Path.Combine(GetDirectory(), $"{gatewayName}.json");
            var json = JsonConvert.SerializeObject(deviceConfigurationBackup, Formatting.Indented);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        public T Get<T>(string gatewayName)
        {
            var path = Path.Combine(GetDirectory(), $"{gatewayName}.json");
            if (!File.Exists(path))
            {
                return default(T);
            }
            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonConvert.DeserializeObject<T>(json);
        }

        private void CreateDirectoryIfNotExisting()
        {
            var directory = GetDirectory();
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private string GetDirectory()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigurationBackup");
        }
    }
}
