using System.Collections.Generic;
using System.Linq;

namespace Xpressive.Home.Contracts.Gateway
{
    public sealed class Action : IAction
    {
        private readonly string _name;
        private readonly List<string> _fields;

        public Action(string name)
        {
            _name = name;
            _fields = new List<string>();
        }

        public string Name => _name;
        public IList<string> Fields => _fields;
    }
}
