using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using NPoco;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Automation
{
    internal class ScheduledScriptRepository : IScheduledScriptRepository
    {
        private readonly DbConnection _dbConnection;

        public ScheduledScriptRepository(DbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task InsertAsync(Guid jobId, Guid scriptId, string cronTab)
        {
            using (var database = new Database(_dbConnection))
            {
                await database.InsertAsync(new ScheduledScript
                {
                    Id = jobId,
                    ScriptId = scriptId,
                    CronTab = cronTab,
                });
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            using (var database = new Database(_dbConnection))
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
            using (var database = new Database(_dbConnection))
            {
                return await database.FetchAsync<ScheduledScript>("select * from ScheduledScript");
            }
        }

        public async Task<ScheduledScript> GetAsync(Guid id)
        {
            using (var database = new Database(_dbConnection))
            {
                return await database.SingleOrDefaultByIdAsync<ScheduledScript>(id);
            }
        }
    }
}
