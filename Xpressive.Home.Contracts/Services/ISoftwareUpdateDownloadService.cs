using System.IO;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Services
{
    public interface ISoftwareUpdateDownloadService
    {
        bool IsNewVersionAvailable();

        Task<FileInfo> DownloadNewestVersionAsync();
    }
}
