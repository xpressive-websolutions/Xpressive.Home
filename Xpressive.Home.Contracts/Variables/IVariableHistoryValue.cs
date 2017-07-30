using System;

namespace Xpressive.Home.Contracts.Variables
{
    public interface IVariableHistoryValue
    {
        DateTime EffectiveDate { get; }
        object Value { get; }
    }
}
