using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPoco;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Automation
{
    internal class ScriptRepository : IScriptRepository
    {
        public async Task SaveAsync(Script script)
        {
            if (string.IsNullOrEmpty(script.Id))
            {
                await InsertAsync(script);
            }
            else
            {
                await UpdateAsync(script);
            }
        }

        public async Task<Script> GetAsync(string id)
        {
            using (var database = new Database("ConnectionString"))
            {
                return await database.SingleOrDefaultByIdAsync<Script>(id);
            }
        }

        public async Task<IEnumerable<Script>> GetAsync(IEnumerable<string> ids)
        {
            var scripts = await GetAsync();
            var scriptIds = new HashSet<string>(ids, StringComparer.Ordinal);
            return scripts.Where(s => scriptIds.Contains(s.Id));
        }

        public async Task<IEnumerable<Script>> GetAsync()
        {
            using (var database = new Database("ConnectionString"))
            {
                return await database.FetchAsync<Script>("select * from Script");
            }
        }

        public async Task DeleteAsync(string id)
        {
            using (var database = new Database("ConnectionString"))
            {
                var dto = await database.SingleOrDefaultByIdAsync<Script>(id);

                if (dto != null)
                {
                    await database.DeleteAsync(dto);
                }
            }
        }

        public async Task DeleteAsync(Script script)
        {
            using (var database = new Database("ConnectionString"))
            {
                await database.DeleteAsync(script);
            }
        }

        private async Task InsertAsync(Script script)
        {
            script.Id = Guid.NewGuid().ToString("n");

            using (var database = new Database("ConnectionString"))
            {
                await database.InsertAsync(script);
            }
        }

        private async Task UpdateAsync(Script script)
        {
            using (var database = new Database("ConnectionString"))
            {
                await database.UpdateAsync(script, new[] {"Name", "JavaScript"});
            }
        }
    }
}