using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Services.Automation
{
    internal class ScheduledScriptRepository : IScheduledScriptRepository
    {
        private readonly IContextFactory _contextFactory;

        public ScheduledScriptRepository(IContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task InsertAsync(string jobId, string scriptId, string cronTab)
        {
            await _contextFactory.InScope(async context =>
            {
                context.ScheduledScript.Add(new ScheduledScript
                {
                    Id = jobId,
                    ScriptId = scriptId,
                    CronTab = cronTab,
                });

                await context.SaveChangesAsync();
            });
        }

        public async Task DeleteAsync(string id)
        {
            await _contextFactory.InScope(async context =>
            {
                var result = await context.ScheduledScript.FindAsync(id);
                context.ScheduledScript.Remove(result);
                await context.SaveChangesAsync();
            });
        }

        public async Task<IEnumerable<ScheduledScript>> GetAsync()
        {
            return await _contextFactory.InScope(async context => await context.ScheduledScript.ToListAsync());
        }

        public Task<ScheduledScript> GetAsync(string id)
        {
            return _contextFactory.InScope(async context => await context.ScheduledScript.FindAsync(id));
        }
    }
}
