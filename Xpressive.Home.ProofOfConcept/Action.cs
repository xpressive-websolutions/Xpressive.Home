using System.Collections.Generic;
using System.Linq;
using Xpressive.Home.ProofOfConcept.Contracts;

namespace Xpressive.Home.ProofOfConcept
{
    internal class Action : IAction
    {
        private readonly string _name;
        private readonly List<string> _fields;

        public Action(string name)
        {
            _name = name;
            _fields = new List<string>();
        }

        public string Name => _name;

        public IList<string> Fields
        {
            get { return _fields.ToList(); }
            set
            {
                _fields.Clear();
                _fields.AddRange(value);
            }
        }
    }
}