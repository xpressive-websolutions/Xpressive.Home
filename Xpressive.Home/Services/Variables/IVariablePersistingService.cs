﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.Services.Variables
{
    internal interface IVariablePersistingService
    {
        void Save(IVariable variable);

        Task<IEnumerable<IVariable>> LoadAsync();
    }
}