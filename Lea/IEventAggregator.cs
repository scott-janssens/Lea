namespace Lea;

public interface IEventAggregator
{
    public delegate void EventAggregatorHandler<T>(T evt) where T : class, IEvent;
    public delegate Task AsyncEventAggregatorHandler<T>(T evt) where T : class, IEvent;

    void Publish(IEvent evt);
    SubscriptionToken Subscribe<T>(EventAggregatorHandler<T> handler) where T : class, IEvent;
    SubscriptionToken Subscribe<T>(AsyncEventAggregatorHandler<T> handler) where T : class, IEvent;
    void Unsubscribe<T>(EventAggregatorHandler<T> handler) where T : class, IEvent;
    void Unsubscribe<T>(AsyncEventAggregatorHandler<T> handler) where T : class, IEvent;
    void Unsubscribe<T>(SubscriptionToken token) where T : class, IEvent;
}