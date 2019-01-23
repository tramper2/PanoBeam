namespace PanoBeam.Events
{
    interface IEventSubscription
    {
        void Execute(object[] arguments);
    }
}