using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Lea.IEventAggregator;

namespace Lea;

/// <summary>
/// Class implementation of Lea Event Aggregator
/// </summary>
public partial class EventAggregator : IEventAggregator, IDisposable
{
    private bool _disposed;
    private readonly ILogger<IEventAggregator>? _logger;
    private readonly Dictionary<Type, SubscriptionList> _subscriptions = new();
    private SynchronizationContext? _syncContext;

    #region LoggerMessages

    [LoggerMessage(LogLevel.Information, "LEA: unable to get recursive write lock in Publish() for event {EventType}; Cleanup not performed.")]
    static partial void LogPublishNoLock(ILogger logger, string eventType);

    [LoggerMessage(LogLevel.Error, "LEA: An error occured invoking event aggregator handler: {msg}")]
    static partial void LogError(ILogger logger, Exception ex, string msg);

    #endregion

    /// <summary>
    /// Constructor
    /// </summary>
    public EventAggregator(SynchronizationContext? synchronizationContext = null)
    {
        _syncContext = synchronizationContext;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">optional logget object</param>
    public EventAggregator(ILogger<IEventAggregator> logger, SynchronizationContext? synchronizationContext = null)
    {
        _logger = logger;
        _syncContext = synchronizationContext;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                foreach (var list in _subscriptions.Values)
                {
                    list.Dispose();
                }
            }

            _disposed = true;
        }
    }

    ~EventAggregator()
    {
        Dispose(false);
    }

    ///<inheritdoc/>
    public SubscriptionToken Subscribe<T>(EventAggregatorHandler<T> handler, SubscriberThread thread = SubscriberThread.PublisherThread)
        where T : class, IEvent
    {
        return GetSubscriptionList(typeof(T)).AddHandler(handler, thread);
    }

    ///<inheritdoc/>
    public SubscriptionToken Subscribe<T>(AsyncEventAggregatorHandler<T> handler, SubscriberThread thread = SubscriberThread.PublisherThread)
           where T : class, IEvent
    {
        return GetSubscriptionList(typeof(T)).AddHandler(handler, thread);
    }

    public SubscriptionToken Subscribe(Type type, EventAggregatorHandler<IEvent> handler, SubscriberThread thread = SubscriberThread.PublisherThread)
    {
        return GetSubscriptionList(type).AddHandler(handler, thread);
    }

    public SubscriptionToken Subscribe(Type type, AsyncEventAggregatorHandler<IEvent> handler, SubscriberThread thread = SubscriberThread.PublisherThread)
    {
        return GetSubscriptionList(type).AddHandler(handler, thread);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SubscriptionList GetSubscriptionList(Type type)
    {
        if (!_subscriptions.TryGetValue(type, out var subscriptionList))
        {
            subscriptionList = new();
            _subscriptions.Add(type, subscriptionList);
        }

        return subscriptionList;
    }

    ///<inheritdoc/>
    public void Unsubscribe<T>(EventAggregatorHandler<T> handler)
        where T : class, IEvent
    {
        Guard.IsNotNull(handler, nameof(handler));
        _subscriptions.GetValueOrDefault(typeof(T))?.RemoveHandler(handler);
    }

    ///<inheritdoc/>
    public void Unsubscribe<T>(AsyncEventAggregatorHandler<T> handler)
         where T : class, IEvent
    {
        Guard.IsNotNull(handler, nameof(handler));
        _subscriptions.GetValueOrDefault(typeof(T))?.RemoveHandler(handler);
    }

    ///<inheritdoc/>
    public void Unsubscribe(Type type, EventAggregatorHandler<IEvent> handler)
    {
        Guard.IsNotNull(handler, nameof(handler));
        _subscriptions.GetValueOrDefault(type)?.RemoveHandler(handler);
    }

    ///<inheritdoc/>
    public void Unsubscribe(Type type, AsyncEventAggregatorHandler<IEvent> handler)
    {
        Guard.IsNotNull(handler, nameof(handler));
        _subscriptions.GetValueOrDefault(type)?.RemoveHandler(handler);
    }

    ///<inheritdoc/>
    public void Unsubscribe<T>(SubscriptionToken token)
          where T : class, IEvent
    {
        Guard.IsNotNull(token, nameof(token));
        _subscriptions.GetValueOrDefault(typeof(T))?.RemoveHandler(token);
    }

    ///<inheritdoc/>
    public void Publish(IEvent evt)
    {
        Guard.IsNotNull(evt, nameof(evt));

        if (_subscriptions.TryGetValue(evt.GetType(), out var subscriptionList))
        {
            try
            {
                subscriptionList.CleanUnreferencedHandlers();
            }
            catch (LockRecursionException)
            {
                if (_logger != null)
                {
                    LogPublishNoLock(_logger, evt.GetType().Name);
                }
            }

            subscriptionList.Invoke(evt, _logger, _syncContext);
        }
    }

    ///<inheritdoc/>
    public Task PublishAsync(IEvent evt)
    {
        Guard.IsNotNull(evt, nameof(evt));
        return Task.Run(() => Publish(evt));
    }

    public void SetSynchronizationContext(SynchronizationContext synchronizationContext)
    {
        Guard.IsNotNull(synchronizationContext, nameof(synchronizationContext));
        _syncContext = synchronizationContext;
    }

    private readonly record struct SubscriptionItem(WeakReference Reference, SubscriberThread Thread);

    private sealed class SubscriptionList : IDisposable
    {
        private readonly ReaderWriterLockSlim _readerWriterLock = new(LockRecursionPolicy.SupportsRecursion);
        private MethodInfo? _invoker;
        private MethodInfo? _asyncInvoker;

        private readonly Dictionary<SubscriptionToken, SubscriptionItem> _references = new();
        private readonly Dictionary<SubscriptionToken, SubscriptionItem> _asyncReferences = new();

        public void Dispose()
        {
            _readerWriterLock.Dispose();
        }

        public SubscriptionToken AddHandler<T>(EventAggregatorHandler<T> handler, SubscriberThread thread)
            where T : class, IEvent
        {
            Guard.IsNotNull(handler, nameof(handler));

            if (!Enum.IsDefined(thread))
            {
                throw new ArgumentOutOfRangeException(nameof(thread));
            }

            if (_invoker == null)
            {
                _invoker = typeof(EventAggregatorHandler<T>).GetMethod("Invoke");
            }

            return AddHandlerInternal(_references, handler, thread);
        }

        public SubscriptionToken AddHandler<T>(AsyncEventAggregatorHandler<T> handler, SubscriberThread thread)
            where T : class, IEvent
        {
            Guard.IsNotNull(handler, nameof(handler));

            if (!Enum.IsDefined(thread))
            {
                throw new ArgumentOutOfRangeException(nameof(thread));
            }

            if (_asyncInvoker == null)
            {
                _asyncInvoker = typeof(AsyncEventAggregatorHandler<T>).GetMethod("Invoke");
            }

            return AddHandlerInternal(_asyncReferences, handler, thread);
        }

        private SubscriptionToken AddHandlerInternal(Dictionary<SubscriptionToken, SubscriptionItem> dict, object handler, SubscriberThread thread)
        {
            SubscriptionToken token;

            _readerWriterLock.EnterWriteLock();

            try
            {
                var pair = dict.FirstOrDefault(x => handler.Equals(x.Value.Reference.Target));

                if (pair.Equals(default(KeyValuePair<SubscriptionToken, SubscriptionItem>)))
                {
                    token = new();
                    dict.Add(token, new SubscriptionItem(new WeakReference(handler, true), thread));
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
                var pair = _references.FirstOrDefault(x => handler.Equals(x.Value.Reference.Target));
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
                var pair = _asyncReferences.FirstOrDefault(x => handler.Equals(x.Value.Reference.Target));
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

        public void Invoke(IEvent evt, ILogger<IEventAggregator>? logger, SynchronizationContext? syncContext)
        {
            ImmutableList<SubscriptionItem> asyncImmutableList;
            ImmutableList<SubscriptionItem> immutableList;

            _readerWriterLock.EnterReadLock();

            try
            {
                asyncImmutableList = _asyncReferences.Values.ToImmutableList();
                immutableList = _references.Values.ToImmutableList();
            }
            finally { _readerWriterLock.ExitReadLock(); }

            foreach (var item in asyncImmutableList)
            {
                switch (item.Thread)
                {
                    case SubscriberThread.PublisherThread:
                        InvokeAwaitInternal(item, evt, logger);
                        break;
                    case SubscriberThread.BackgroundThread:
                        Task.Run(() => InvokeAwaitInternal(item, evt, logger));
                        break;
                    case SubscriberThread.ContextThread:
                        if (syncContext == null)
                        {
                            if (logger != null)
                            {
                                var ex = new InvalidOperationException($"A SynchronizationContext must be set to invoke a subescriber with option {nameof(SubscriberThread.ContextThread)}.");
                                LogError(logger, ex, ex.ToString());
                            }
                        }
                        else
                        {
                            syncContext.Post(_ => InvokeAwaitInternal(item, evt, logger), null);
                        }
                        break;
                }
            }

            // TODO: Add throw on error which aggregates exceptions

            foreach (var item in immutableList)
            {
                switch (item.Thread)
                {
                    case SubscriberThread.PublisherThread:
                        InvokeInternal(_invoker!, item, evt, logger);
                        break;
                    case SubscriberThread.BackgroundThread:
                        Task.Run(() => InvokeInternal(_invoker!, item, evt, logger));
                        break;
                    case SubscriberThread.ContextThread:
                        if (syncContext == null)
                        {
                            if (logger != null)
                            {
                                var ex = new InvalidOperationException($"A SynchronizationContext must be set to invoke a subescriber with option {nameof(SubscriberThread.ContextThread)}.");
                                LogError(logger, ex, ex.ToString());
                            }
                        }
                        else
                        {
                            syncContext.Post(_ => InvokeInternal(_invoker!, item, evt, logger), null);
                        }
                        break;
                }
            }
        }

        private static void InvokeInternal(MethodInfo info, SubscriptionItem item, IEvent evt, ILogger<IEventAggregator>? logger)
        {
            if (item.Reference.Target != null)
            {
#pragma warning disable CA1031 // Do not catch general exception types
                try
                {
                    info.Invoke(item.Reference.Target, new[] { evt });
                }
                catch (Exception ex)
                {
                    if (logger != null)
                    {
                        LogError(logger, ex, ex.ToString());
                    }
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }

        private void InvokeAwaitInternal(SubscriptionItem reference, IEvent evt, ILogger<IEventAggregator>? logger)
        {
            if (reference.Reference.Target != null)
            {
                ((Task)_asyncInvoker!.Invoke(reference.Reference.Target, new[] { evt })!).ContinueWith(t =>
                {
                    t.Exception?.Handle(e =>
                    {
                        if (logger != null)
                        {
                            LogError(logger, e, e.ToString());
                        }

                        return true;
                    });
                }, TaskScheduler.Current);
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

        private static void CleanUnreferencedHandlersInternal(Dictionary<SubscriptionToken, SubscriptionItem> dict)
        {
            var pairs = dict.Where(x => x.Value.Reference.Target == null);

            foreach (var pair in pairs)
            {
                dict.Remove(pair.Key);
            }
        }
    }
}
