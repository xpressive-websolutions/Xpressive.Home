namespace Xpressive.Home.Contracts.Variables
{
    public interface IVariable
    {
        string Name { get; set; }

        object Value { get; set; }
    }
}