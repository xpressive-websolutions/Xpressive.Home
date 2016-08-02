using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NPoco;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Automation
{
    internal sealed class ScriptTriggerService : IScriptTriggerService
    {
        public async Task<IEnumerable<TriggeredScript>> GetTriggersAsync()
        {
            using (var database = new Database("ConnectionString"))
            {
                const string sql = "select * from TriggeredScript";
                return await database.FetchAsync<TriggeredScript>(sql);
            }
        }

        public async Task<IEnumerable<TriggeredScript>> GetTriggersByVariableAsync(string variable)
        {
            using (var database = new Database("ConnectionString"))
            {
                const string sql = "select * from TriggeredScript where [Variable] = @0";
                return await database.FetchAsync<TriggeredScript>(sql, variable);
            }
        }

        public async Task<IEnumerable<TriggeredScript>> GetTriggersByScriptAsync(string scriptId)
        {
            using (var database = new Database("ConnectionString"))
            {
                const string sql = "select * from TriggeredScript where ScriptId = @0";
                return await database.FetchAsync<TriggeredScript>(sql, scriptId);
            }
        }

        public async Task<TriggeredScript> AddTriggerAsync(string scriptId, string variable)
        {
            var triggeredScript = new TriggeredScript
            {
                Id = Guid.NewGuid().ToString("n"),
                ScriptId = scriptId,
                Variable = variable
            };

            using (var database = new Database("ConnectionString"))
            {
                await database.InsertAsync(triggeredScript);
            }

            return triggeredScript;
        }

        public async Task DeleteTriggerAsync(string id)
        {
            using (var database = new Database("ConnectionString"))
            {
                var dto = await database.SingleOrDefaultByIdAsync<TriggeredScript>(id);

                if (dto != null)
                {
                    await database.DeleteAsync(dto);
                }
            }
        }
    }
}
