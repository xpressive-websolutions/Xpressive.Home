using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Services.Automation
{
    internal class ScriptRepository : IScriptRepository
    {
        private readonly IContextFactory _contextFactory;

        public ScriptRepository(IContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task SaveAsync(Script script)
        {
            script.ShouldNotBeNull();

            if (Guid.Empty.Equals(script.Id))
            {
                await InsertAsync(script);
            }
            else
            {
                await UpdateAsync(script);
            }
        }

        public Task<Script> GetAsync(Guid id)
        {
            return _contextFactory.InScope(async context => await context.Script.FindAsync(id));
        }

        public async Task<IEnumerable<Script>> GetAsync(IEnumerable<Guid> ids)
        {
            var scripts = await GetAsync();
            var scriptIds = new HashSet<Guid>(ids);
            return scripts.Where(s => scriptIds.Contains(s.Id));
        }

        public async Task<IEnumerable<Script>> GetAsync()
        {
            return await _contextFactory.InScope(async context => await context.Script.ToListAsync());
        }

        public async Task EnableAsync(Script script)
        {
            script.ShouldNotBeNull();

            await _contextFactory.InScope(async context =>
            {
                var s = await context.Script.FindAsync(script.Id);
                s.IsEnabled = true;
                await context.SaveChangesAsync();
            });
        }

        public async Task DisableAsync(Script script)
        {
            script.ShouldNotBeNull();

            await _contextFactory.InScope(async context =>
            {
                var s = await context.Script.FindAsync(script.Id);
                s.IsEnabled = false;
                await context.SaveChangesAsync();
            });
        }

        public async Task DeleteAsync(Guid id)
        {
            await _contextFactory.InScope(async context =>
            {
                var script = await context.Script.FindAsync(id);
                if (script != null)
                {
                    context.Script.Remove(script);
                    await context.SaveChangesAsync();
                }
            });
        }

        public async Task DeleteAsync(Script script)
        {
            script.ShouldNotBeNull();

            await _contextFactory.InScope(async context =>
            {
                context.Script.Remove(script);
                await context.SaveChangesAsync();
            });
        }

        private async Task InsertAsync(Script script)
        {
            script.ShouldNotBeNull();

            script.Id = Guid.NewGuid();

            await _contextFactory.InScope(async context =>
            {
                context.Script.Add(script);
                await context.SaveChangesAsync();
            });
        }

        private async Task UpdateAsync(Script script)
        {
            script.ShouldNotBeNull();

            await _contextFactory.InScope(async context =>
            {
                context.Script.Attach(script);
                await context.SaveChangesAsync();
            });
        }
    }
}
