namespace Xpressive.Home.Contracts.Services
{
    public interface IDeviceConfigurationBackupService
    {
        void Save<T>(string gatewayName, T deviceConfigurationBackup);
        T Get<T>(string gatewayName);
    }
}
