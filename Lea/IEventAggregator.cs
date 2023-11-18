namespace Lea;

/// <summary>
/// Interface for Lea EventAggregator
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    /// Delegate for synchronous event handlers
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="evt">the published event object</param>
    public delegate void EventAggregatorHandler<T>(T evt) where T : class, IEvent;

    /// <summary>
    /// Delegate for asynchronous event handlers
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="evt">the published event object</param>
    /// <returns>Task</returns>
    public delegate Task AsyncEventAggregatorHandler<T>(T evt) where T : class, IEvent;

    /// <summary>
    /// Publishes an event object.  All handlers subscribed for the event type will be called and passed this event object.
    /// </summary>
    /// <param name="evt">event object</param>
    void Publish(IEvent evt);
    
    /// <summary>
    /// Subscribes a synchronous handler method to recieve events of Type T.
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="handler">synchronous handler method</param>
    /// <returns>A token object which can be used to unsubscribe the handler</returns>
    SubscriptionToken Subscribe<T>(EventAggregatorHandler<T> handler) where T : class, IEvent;

    /// <summary>
    /// Subscribes an asynchronous handler method to recieve events of Type T.
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="handler">asynchronous handler method</param>
    /// <returns>A token object which can be used to unsubscribe the handler</returns>
    SubscriptionToken Subscribe<T>(AsyncEventAggregatorHandler<T> handler) where T : class, IEvent;

    /// <summary>
    /// Unsubscribes a synchronous event handler method so that it will no longer be called.
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="handler">synchronous handler method</param>
    void Unsubscribe<T>(EventAggregatorHandler<T> handler) where T : class, IEvent;

    /// <summary>
    /// Unsubscribes an asynchronous event handler method so that it will no longer be called.
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="handler">asynchronous handler method</param>
    void Unsubscribe<T>(AsyncEventAggregatorHandler<T> handler) where T : class, IEvent;

    /// <summary>
    /// Unsubscribes the event handler method by the token returned when it was subscribed.
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="token">token object returned by <see cref="Subscribe"/></param>
    void Unsubscribe<T>(SubscriptionToken token) where T : class, IEvent;
}