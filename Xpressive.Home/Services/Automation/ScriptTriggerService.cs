using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Services.Automation
{
    internal sealed class ScriptTriggerService : IScriptTriggerService
    {
        private readonly IContextFactory _contextFactory;

        public ScriptTriggerService(IContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IEnumerable<TriggeredScript>> GetTriggersAsync()
        {
            return await _contextFactory.InScope(async context => await context.TriggeredScript.ToListAsync());
        }

        public async Task<IEnumerable<TriggeredScript>> GetTriggersByVariableAsync(string variable)
        {
            return await _contextFactory.InScope(async context => await context.TriggeredScript.Where(t => t.Variable == variable).ToListAsync());
        }

        public async Task<IEnumerable<TriggeredScript>> GetTriggersByScriptAsync(string scriptId)
        {
            return await _contextFactory.InScope(async context => await context.TriggeredScript.Where(t => t.ScriptId == scriptId).ToListAsync());
        }

        public async Task<TriggeredScript> AddTriggerAsync(string scriptId, string variable)
        {
            var triggeredScript = new TriggeredScript
            {
                Id = Guid.NewGuid().ToString("n"),
                ScriptId = scriptId,
                Variable = variable
            };

            await _contextFactory.InScope(async context =>
            {
                context.TriggeredScript.Add(triggeredScript);
                await context.SaveChangesAsync();
            });

            return triggeredScript;
        }

        public async Task DeleteTriggerAsync(string id)
        {
            await _contextFactory.InScope(async context =>
            {
                var t = await context.TriggeredScript.FindAsync(id);
                if (t != null)
                {
                    context.TriggeredScript.Remove(t);
                    await context.SaveChangesAsync();
                }
            });

        }
    }
}
