using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Threading;
using static Lea.IEventAggregator;

namespace Lea;

/// <summary>
/// Class implementation of Lea Event Aggregator
/// </summary>
public class EventAggregator : IEventAggregator
{
    private readonly ILogger<IEventAggregator>? _logger;
    private readonly Dictionary<Type, SubscriptionList> _subscriptions = new();

    /// <summary>
    /// Constructor
    /// </summary>
    public EventAggregator()
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">optional logget object</param>
    public EventAggregator(ILogger<IEventAggregator> logger)
    {
        _logger = logger;
    }

    ///<inheritdoc/>
    public SubscriptionToken Subscribe<T>(EventAggregatorHandler<T> handler)
        where T : class, IEvent
    {
        Guard.IsNotNull(handler);

        if (!_subscriptions.TryGetValue(typeof(T), out var subscriptionList))
        {
            subscriptionList = new();
            _subscriptions.Add(typeof(T), subscriptionList);
        }

        return subscriptionList.AddHandler(handler);
    }

    ///<inheritdoc/>
    public SubscriptionToken Subscribe<T>(AsyncEventAggregatorHandler<T> handler)
           where T : class, IEvent
    {
        Guard.IsNotNull(handler);

        if (!_subscriptions.TryGetValue(typeof(T), out var subscriptionList))
        {
            subscriptionList = new();
            _subscriptions.Add(typeof(T), subscriptionList);
        }

        return subscriptionList.AddHandler(handler);
    }

    ///<inheritdoc/>
    public void Unsubscribe<T>(EventAggregatorHandler<T> handler)
        where T : class, IEvent
    {
        Guard.IsNotNull(handler);
        _subscriptions.GetValueOrDefault(typeof(T))?.RemoveHandler(handler);
    }

    ///<inheritdoc/>
    public void Unsubscribe<T>(AsyncEventAggregatorHandler<T> handler)
         where T : class, IEvent
    {
        Guard.IsNotNull(handler);
        _subscriptions.GetValueOrDefault(typeof(T))?.RemoveHandler(handler);
    }

    ///<inheritdoc/>
    public void Unsubscribe<T>(SubscriptionToken token)
          where T : class, IEvent
    {
        Guard.IsNotNull(token);
        _subscriptions.GetValueOrDefault(typeof(T))?.RemoveHandler(token);
    }

    ///<inheritdoc/>
    public void Publish(IEvent evt)
    {
        Guard.IsNotNull(evt);

        if (_subscriptions.TryGetValue(evt.GetType(), out var subscriptionList))
        {
            try
            {
                subscriptionList.CleanUnreferencedHandlers();
            }
            catch (LockRecursionException)
            {
                _logger?.LogInformation("LEA: unable to get recursive write lock in Publish() for event {eventType}; Cleanup not performed.", evt.GetType().Name);
            }

            subscriptionList.Invoke(evt, _logger);
        }
    }

    ///<inheritdoc/>
    public Task PublishAsync(IEvent evt)
    {
        Guard.IsNotNull(evt);

        return Task.Run(() => Publish(evt));
    }

    private class SubscriptionList
    {
        private readonly ReaderWriterLockSlim _readerWriterLock = new(LockRecursionPolicy.SupportsRecursion);
        private MethodInfo? _invoker = null;
        private MethodInfo? _asyncInvoker = null;

        private readonly Dictionary<SubscriptionToken, WeakReference> _references = new();
        private readonly Dictionary<SubscriptionToken, WeakReference> _asyncReferences = new();

        public SubscriptionToken AddHandler<T>(EventAggregatorHandler<T> handler)
            where T : class, IEvent
        {
            if (_invoker == null)
            {
                _invoker = typeof(EventAggregatorHandler<T>).GetMethod("Invoke");
            }

            return AddHandlerInternal(_references, handler);
        }

        public SubscriptionToken AddHandler<T>(AsyncEventAggregatorHandler<T> handler)
            where T : class, IEvent
        {
            if (_asyncInvoker == null)
            {
                _asyncInvoker = typeof(AsyncEventAggregatorHandler<T>).GetMethod("Invoke");
            }

            return AddHandlerInternal(_asyncReferences, handler);
        }

        private SubscriptionToken AddHandlerInternal(Dictionary<SubscriptionToken, WeakReference> dict, object handler)
        {
            SubscriptionToken token;

            _readerWriterLock.EnterWriteLock();

            try
            {
                var pair = dict.FirstOrDefault(x => handler.Equals(x.Value.Target));

                if (pair.Equals(default(KeyValuePair<SubscriptionToken, WeakReference>)))
                {
                    token = new();
                    dict.Add(token, new WeakReference(handler, true));
                }
                else
                {
                    token = pair.Key;
                }

                return token;
            }
            finally { _readerWriterLock.ExitWriteLock(); }
        }

        public void RemoveHandler<T>(EventAggregatorHandler<T> handler)
            where T : class, IEvent
        {
            _readerWriterLock.EnterWriteLock();

            try
            {
                var pair = _references.FirstOrDefault(x => handler.Equals(x.Value.Target));
                _references.Remove(pair.Key);
            }
            finally { _readerWriterLock.ExitWriteLock(); }
        }

        public void RemoveHandler<T>(AsyncEventAggregatorHandler<T> handler)
            where T : class, IEvent
        {
            _readerWriterLock.EnterWriteLock();

            try
            {
                var pair = _asyncReferences.FirstOrDefault(x => handler.Equals(x.Value.Target));
                _asyncReferences.Remove(pair.Key);
            }
            finally { _readerWriterLock.ExitWriteLock(); }
        }

        public void RemoveHandler(SubscriptionToken token)
        {
            _readerWriterLock.EnterWriteLock();

            try
            {
                _references.Remove(token);
                _asyncReferences.Remove(token);
            }
            finally { _readerWriterLock.ExitWriteLock(); }
        }

        public void Invoke(IEvent evt, ILogger<IEventAggregator>? logger)
        {
            _readerWriterLock.EnterReadLock();

            try
            {
                foreach (var reference in _asyncReferences.Values)
                {
                    InvokeAwaitInternal(reference, evt, logger);
                }

                foreach (var reference in _references.Values)
                {
                    InvokeInternal(_invoker!, reference, evt, logger);
                }
            }
            finally { _readerWriterLock.ExitReadLock(); }
        }

        private static void InvokeInternal(MethodInfo info, WeakReference reference, IEvent evt, ILogger<IEventAggregator>? logger)
        {
            if (reference.Target != null)
            {
                try
                {
                    info.Invoke(reference.Target, new[] { evt });
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "An error occured invoking event aggregator handler");
                }
            }
        }

        private void InvokeAwaitInternal(WeakReference reference, IEvent evt, ILogger<IEventAggregator>? logger)
        {
            if (reference.Target != null)
            {
                ((Task)_asyncInvoker!.Invoke(reference.Target, new[] { evt })!).ContinueWith(t =>
                {
                    t.Exception?.Handle(e =>
                    {
                        logger?.LogError(e, "An error occured invoking event aggregator handler");
                        return true;
                    });
                });
            }
        }

        public void CleanUnreferencedHandlers()
        {
            _readerWriterLock.EnterWriteLock();

            try
            {
                CleanUnreferencedHandlersInternal(_asyncReferences);
                CleanUnreferencedHandlersInternal(_references);
            }
            finally { _readerWriterLock.ExitWriteLock(); }
        }

        private static void CleanUnreferencedHandlersInternal(Dictionary<SubscriptionToken, WeakReference> dict)
        {
            var pairs = dict.Where(x => x.Value.Target == null);

            foreach (var pair in pairs)
            {
                dict.Remove(pair.Key);
            }
        }
    }
}
