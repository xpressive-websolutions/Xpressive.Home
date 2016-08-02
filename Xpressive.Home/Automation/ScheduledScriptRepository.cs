using System.Collections.Generic;
using System.Threading.Tasks;
using NPoco;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Automation
{
    internal class ScheduledScriptRepository : IScheduledScriptRepository
    {
        public async Task InsertAsync(string jobId, string scriptId, string cronTab)
        {
            using (var database = new Database("ConnectionString"))
            {
                await database.InsertAsync(new ScheduledScript
                {
                    Id = jobId,
                    ScriptId = scriptId,
                    CronTab = cronTab,
                });
            }
        }

        public async Task DeleteAsync(string id)
        {
            using (var database = new Database("ConnectionString"))
            {
                var dto = await database.SingleOrDefaultByIdAsync<ScheduledScript>(id);

                if (dto != null)
                {
                    await database.DeleteAsync(dto);
                }
            }
        }

        public async Task<IEnumerable<ScheduledScript>> GetAsync()
        {
            using (var database = new Database("ConnectionString"))
            {
                return await database.FetchAsync<ScheduledScript>("select * from ScheduledScript");
            }
        }

        public async Task<ScheduledScript> GetAsync(string id)
        {
            using (var database = new Database("ConnectionString"))
            {
                return await database.SingleOrDefaultByIdAsync<ScheduledScript>(id);
            }
        }
    }
}
