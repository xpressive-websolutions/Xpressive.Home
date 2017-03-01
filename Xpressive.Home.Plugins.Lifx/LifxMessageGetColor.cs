namespace Xpressive.Home.Plugins.Lifx
{
    internal class LifxMessageGetColor : LifxMessage
    {
        public LifxMessageGetColor() : base(101)
        {
            Address.ResponseRequired = true;
        }
    }
}
