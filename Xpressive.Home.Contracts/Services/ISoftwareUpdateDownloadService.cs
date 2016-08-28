using System.IO;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Services
{
    public interface ISoftwareUpdateDownloadService
    {
        Task<bool> IsNewVersionAvailableAsync();

        Task<FileInfo> DownloadNewestVersionAsync();
    }
}
