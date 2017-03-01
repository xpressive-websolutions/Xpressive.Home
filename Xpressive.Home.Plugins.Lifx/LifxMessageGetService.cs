namespace Xpressive.Home.Plugins.Lifx
{
    internal class LifxMessageGetService : LifxMessage
    {
        public LifxMessageGetService() : base(2)
        {
            Frame.Tagged = true;
        }
    }
}
