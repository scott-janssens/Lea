# Lite Event Aggregator (Lea)
Lea is an implementation of the event aggregator pattern which decouples event senders and consumers.  Consuming classes subscribe to events by registering handler methods.  Lea supports both synchronous and asynchronous handlers.

```
{
  eventAggregactor.Subscribe<MyEvent>(MyEventHandler);
  eventAggregactor.Subscribe<MyEvent>(AsyncMyEventHandler);
}

private void MyEventHandler(MyEvent evt)
{
  // handling code here
}


private async Task AsyncMyEventHandler(MyEvent evt)
{
  // handling code here
}
```

The handler is called by EventAggregator whenever the MyEvent event is published:

```
{
  eventAggregator.Publish(new MyEvent());
}
```

Events must be classes that implement the Lea.IEvent interface.  There are no other restrictions on the event class implementation.

```
public class MyEvent : IEvent
{
  public string Value { get; set; }
}
```

Lea keeps only weak references to handlers so will not prevent garbage collection of any object with a method registered as a handler.
