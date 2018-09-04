using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using NPoco;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Automation
{
    internal sealed class ScriptTriggerService : IScriptTriggerService
    {
        private readonly DbConnection _dbConnection;

        public ScriptTriggerService(DbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<TriggeredScript>> GetTriggersAsync()
        {
            using (var database = new Database(_dbConnection))
            {
                const string sql = "select * from TriggeredScript";
                return await database.FetchAsync<TriggeredScript>(sql);
            }
        }

        public async Task<IEnumerable<TriggeredScript>> GetTriggersByVariableAsync(string variable)
        {
            using (var database = new Database(_dbConnection))
            {
                const string sql = "select * from TriggeredScript where [Variable] = @0";
                return await database.FetchAsync<TriggeredScript>(sql, variable);
            }
        }

        public async Task<IEnumerable<TriggeredScript>> GetTriggersByScriptAsync(Guid scriptId)
        {
            using (var database = new Database(_dbConnection))
            {
                const string sql = "select * from TriggeredScript where ScriptId = @0";
                return await database.FetchAsync<TriggeredScript>(sql, scriptId);
            }
        }

        public async Task<TriggeredScript> AddTriggerAsync(Guid scriptId, string variable)
        {
            var triggeredScript = new TriggeredScript
            {
                Id = Guid.NewGuid(),
                ScriptId = scriptId,
                Variable = variable
            };

            using (var database = new Database(_dbConnection))
            {
                await database.InsertAsync(triggeredScript);
            }

            return triggeredScript;
        }

        public async Task DeleteTriggerAsync(Guid id)
        {
            using (var database = new Database(_dbConnection))
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
