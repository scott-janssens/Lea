﻿namespace Lea;

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
    public delegate void EventAggregatorHandler(IEvent evt);

    /// <summary>
    /// Delegate for asynchronous event handlers
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="evt">the published event object</param>
    /// <returns>Task</returns>
    public delegate Task AsyncEventAggregatorHandler(IEvent evt);

    /// <summary>
    /// Generic delegate for synchronous event handlers
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="evt">the published event object</param>
    public delegate void EventAggregatorHandler<in T>(T evt) where T : class, IEvent;

    /// <summary>
    /// Generic delegate for asynchronous event handlers
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="evt">the published event object</param>
    /// <returns>Task</returns>
    public delegate Task AsyncEventAggregatorHandler<in T>(T evt) where T : class, IEvent;

    /// <summary>
    /// Publishes an event object.  All handlers subscribed for the event type will be called and passed this event object. Exceptions in
    /// handlers will be logged and swallowed.
    /// </summary>
    /// <param name="evt">event object</param>
    /// <exception cref="ArgumentNullException">evt is null.</exception>
    void Publish(IEvent evt);

    /// <summary>
    /// Publishes an event object.  All handlers subscribed for the event type will be called and passed this event object. Exceptions in
    /// handlers will be logged and swallowed.
    /// </summary>
    /// <param name="evt">event object</param>
    /// <exception cref="ArgumentNullException">evt is null.</exception>
    /// <returns>Task</returns>
    Task PublishAsync(IEvent evt);

    /// <summary>
    /// Subscribes a synchronous handler method to recieve events of Type T.
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="handler">synchronous handler method</param>
    /// <exception cref="ArgumentNullException">handler is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Invalid SubscriberThread value.</exception>
    /// <returns>A token object which can be used to unsubscribe the handler</returns>
    SubscriptionToken Subscribe<T>(EventAggregatorHandler<T> handler, SubscriberThread thread = SubscriberThread.PublisherThread) where T : class, IEvent;

    /// <summary>
    /// Subscribes an asynchronous handler method to recieve events of Type T.
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="handler">asynchronous handler method</param>
    /// <exception cref="ArgumentNullException">handler is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Invalid SubscriberThread value.</exception>
    /// <returns>A token object which can be used to unsubscribe the handler</returns>
    SubscriptionToken Subscribe<T>(AsyncEventAggregatorHandler<T> handler, SubscriberThread thread = SubscriberThread.PublisherThread) where T : class, IEvent;

    /// <summary>
    /// Subscribes a synchronous handler method to recieve events of Type T.
    /// </summary>
    /// <param name="type">event Type</typeparam>
    /// <param name="handler">synchronous handler method</param>
    /// <exception cref="ArgumentNullException">handler is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Invalid SubscriberThread value.</exception>
    /// <returns>A token object which can be used to unsubscribe the handler</returns>
    public SubscriptionToken Subscribe(Type type, EventAggregatorHandler<IEvent> handler, SubscriberThread thread = SubscriberThread.PublisherThread);

    /// <summary>
    /// Subscribes an asynchronous handler method to recieve events of Type T.
    /// </summary>
    /// <param name="type">event Type</typeparam>
    /// <param name="handler">asynchronous handler method</param>
    /// <exception cref="ArgumentNullException">handler is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Invalid SubscriberThread value.</exception>
    /// <returns>A token object which can be used to unsubscribe the handler</returns>
    public SubscriptionToken Subscribe(Type type, AsyncEventAggregatorHandler<IEvent> handler, SubscriberThread thread = SubscriberThread.PublisherThread);

    /// <summary>
    /// Unsubscribes a synchronous event handler method so that it will no longer be called.
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="handler">synchronous handler method</param>
    /// <exception cref="ArgumentNullException">handler is null.</exception>
    void Unsubscribe<T>(EventAggregatorHandler<T> handler) where T : class, IEvent;

    /// <summary>
    /// Unsubscribes an asynchronous event handler method so that it will no longer be called.
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="handler">asynchronous handler method</param>
    /// <exception cref="ArgumentNullException">handler is null.</exception>
    void Unsubscribe<T>(AsyncEventAggregatorHandler<T> handler) where T : class, IEvent;

    /// <summary>
    /// Unsubscribes a synchronous event handler method so that it will no longer be called.
    /// </summary>
    /// <param name="type">event Type</typeparam>
    /// <param name="handler">synchronous handler method</param>
    /// <exception cref="ArgumentNullException">handler is null.</exception>
    void Unsubscribe(Type type, EventAggregatorHandler<IEvent> handler);

    /// <summary>
    /// Unsubscribes an asynchronous event handler method so that it will no longer be called.
    /// </summary>
    /// <param name="type">event Type</typeparam>
    /// <param name="handler">asynchronous handler method</param>
    /// <exception cref="ArgumentNullException">handler is null.</exception>
    void Unsubscribe(Type type, AsyncEventAggregatorHandler<IEvent> handler);

    /// <summary>
    /// Unsubscribes the event handler method by the token returned when it was subscribed.
    /// </summary>
    /// <typeparam name="T">event Type</typeparam>
    /// <param name="token">token object returned by <see cref="Subscribe"/></param>
    /// <exception cref="ArgumentNullException">handler is null.</exception>
    void Unsubscribe<T>(SubscriptionToken token) where T : class, IEvent;

    /// <summary>
    /// Sets the context to execute event handlers specified with SubscriberThread.ContextThread.
    /// </summary>
    /// <param name="synchronizationContext">A SynchronizationContext object.</param>
    /// <exception cref="ArgumentNullException">synchronizationContext is null.</exception>
    void SetSynchronizationContext(SynchronizationContext synchronizationContext);
}