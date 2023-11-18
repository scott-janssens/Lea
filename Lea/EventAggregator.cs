using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Reflection;
using static Lea.IEventAggregator;

namespace Lea;

public class EventAggregator : IEventAggregator
{
    private readonly ILogger<IEventAggregator>? _logger;
    private readonly ReaderWriterLock readerWriterLock = new();
    private readonly Dictionary<Type, SubscriptionList> _subscriptions = new();

    public EventAggregator()
    {
    }

    public EventAggregator(ILogger<IEventAggregator> logger)
    {
        _logger = logger;
    }

    public SubscriptionToken Subscribe<T>(EventAggregatorHandler<T> handler)
        where T : class, IEvent
    {
        Guard.IsNotNull(handler);

        readerWriterLock.AcquireWriterLock(Timeout.Infinite);

        try
        {
            if (!_subscriptions.TryGetValue(typeof(T), out var subscriptionList))
            {
                subscriptionList = new();
                _subscriptions.Add(typeof(T), subscriptionList);
            }

            return subscriptionList.AddHandler(handler);
        }
        finally { readerWriterLock.ReleaseWriterLock(); }
    }

    public SubscriptionToken Subscribe<T>(AsyncEventAggregatorHandler<T> handler)
           where T : class, IEvent
    {
        Guard.IsNotNull(handler);

        readerWriterLock.AcquireWriterLock(Timeout.Infinite);

        try
        {
            if (!_subscriptions.TryGetValue(typeof(T), out var subscriptionList))
            {
                subscriptionList = new();
                _subscriptions.Add(typeof(T), subscriptionList);
            }

            return subscriptionList.AddHandler(handler);
        }
        finally { readerWriterLock.ReleaseWriterLock(); }
    }

    public void Unsubscribe<T>(EventAggregatorHandler<T> handler)
        where T : class, IEvent
    {
        Guard.IsNotNull(handler);

        readerWriterLock.AcquireWriterLock(Timeout.Infinite);

        try
        {
            _subscriptions.GetValueOrDefault(typeof(T))?.RemoveHandler(handler);
        }
        finally { readerWriterLock.ReleaseWriterLock(); }
    }

    public void Unsubscribe<T>(AsyncEventAggregatorHandler<T> handler)
         where T : class, IEvent
    {
        Guard.IsNotNull(handler);

        readerWriterLock.AcquireWriterLock(Timeout.Infinite);

        try
        {
            _subscriptions.GetValueOrDefault(typeof(T))?.RemoveHandler(handler);
        }
        finally { readerWriterLock.ReleaseWriterLock(); }
    }

    public void Unsubscribe<T>(SubscriptionToken token)
          where T : class, IEvent
    {
        Guard.IsNotNull(token);

        readerWriterLock.AcquireReaderLock(Timeout.Infinite);

        try
        {
            _subscriptions.GetValueOrDefault(typeof(T))?.RemoveHandler(token);
        }
        finally { readerWriterLock.ReleaseReaderLock(); }
    }

    public void Publish(IEvent evt)
    {
        Guard.IsNotNull(evt);

        if (_subscriptions.TryGetValue(evt.GetType(), out var subscriptionList))
        {
            readerWriterLock.AcquireWriterLock(Timeout.Infinite);

            try
            {
                subscriptionList.CleanUnreferencedHandlers();
            }
            finally { readerWriterLock.ReleaseReaderLock(); }

            readerWriterLock.AcquireReaderLock(Timeout.Infinite);

            try
            {
                subscriptionList.Invoke(evt, _logger);
            }
            finally { readerWriterLock.ReleaseReaderLock(); }
        }
    }

    private class SubscriptionList
    {
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

        private static SubscriptionToken AddHandlerInternal(Dictionary<SubscriptionToken, WeakReference> dict, object handler)
        {
            SubscriptionToken token;
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

        public void RemoveHandler<T>(EventAggregatorHandler<T> handler)
            where T : class, IEvent
        {
            var pair = _references.FirstOrDefault(x => handler.Equals(x.Value.Target));
            _references.Remove(pair.Key);
        }

        public void RemoveHandler<T>(AsyncEventAggregatorHandler<T> handler)
            where T : class, IEvent
        {
            var pair = _asyncReferences.FirstOrDefault(x => handler.Equals(x.Value.Target));
            _asyncReferences.Remove(pair.Key);
        }

        public void RemoveHandler(SubscriptionToken token)
        {
            _references.Remove(token);
            _asyncReferences.Remove(token);
        }

        public void Invoke(IEvent evt, ILogger<IEventAggregator>? logger)
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
            CleanUnreferencedHandlersInternal(_asyncReferences);
            CleanUnreferencedHandlersInternal(_references);
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
