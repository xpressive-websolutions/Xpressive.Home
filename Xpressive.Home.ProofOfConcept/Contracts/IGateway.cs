using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.ProofOfConcept.Contracts
{
    public interface IGateway
    {
        string Name { get; }
        IEnumerable<IDevice> Devices { get; }
        IEnumerable<IAction> Actions { get; }
        //IEnumerable<IProperty> Properties { get; }

        bool IsConfigurationValid();

        Task Execute(IDeviceAction action);
    }
}