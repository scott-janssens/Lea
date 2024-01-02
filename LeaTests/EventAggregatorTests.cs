using Lea;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics.CodeAnalysis;
using static Lea.IEventAggregator;

namespace LeaTests
{
    [ExcludeFromCodeCoverage]
    public class Tests
    {
        private IEventAggregator _lea;

        [SetUp]
        public void Setup()
        {
            _lea = new EventAggregator();
        }

        [Test]
        public void Publish()
        {
            var handlerCalled = 0;
            string? lastValue = null;
            var publishThreadId = Environment.CurrentManagedThreadId;
            int handlerThreadId = publishThreadId;

            void Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
                handlerThreadId = Environment.CurrentManagedThreadId;
            }

            _lea.Subscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent() { Value = "one" });
            _lea.Unsubscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
                Assert.That(publishThreadId, Is.EqualTo(handlerThreadId));
            });
        }

        [Test]
        public async Task PublishAsync()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
            }

            _lea.Subscribe<TestEvent>(Handler);
            await _lea.PublishAsync(new TestEvent() { Value = "one" });
            _lea.Unsubscribe<TestEvent>(Handler);
            await _lea.PublishAsync(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void SubscribeNonGeneric()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Handler(IEvent evt)
            {
                handlerCalled++;
                lastValue = ((TestEvent)evt).Value;
            }

            _lea.Subscribe(typeof(TestEvent), Handler);
            _lea.Publish(new TestEvent() { Value = "one" });
            _lea.Unsubscribe(typeof(TestEvent), Handler);
            _lea.Publish(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public async Task SubscribeNonGenericAsync()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            async Task Handler(IEvent evt)
            {
                handlerCalled++;
                lastValue = ((TestEvent)evt).Value;
                await Task.Delay(1);
            }

            _lea.Subscribe(typeof(TestEvent), Handler);
            await _lea.PublishAsync(new TestEvent() { Value = "one" });
            _lea.Unsubscribe(typeof(TestEvent), Handler);
            await _lea.PublishAsync(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void PublishAsyncHandler()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            async Task Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
                await Task.Delay(1);
            }

            _lea.Subscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent() { Value = "one" });
            _lea.Unsubscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void DoubleSubscribe()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
            }

            var token1 = _lea.Subscribe<TestEvent>(Handler);
            var token2 = _lea.Subscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent() { Value = "one" });
            _lea.Unsubscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(token1, Is.SameAs(token2));
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void DoubleSubscribeAsyncHandler()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            async Task Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
                await Task.Delay(1);
            }

            var token1 = _lea.Subscribe<TestEvent>(Handler);
            var token2 = _lea.Subscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent() { Value = "one" });
            _lea.Unsubscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(token1, Is.SameAs(token2));
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void UnsubscribeWithToken()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
            }

            var token = _lea.Subscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent() { Value = "one" });
            _lea.Unsubscribe<TestEvent>(token);
            _lea.Publish(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void UnsubscribeWithTokenAsyncHandler()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            async Task Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
                await Task.Delay(1);
            }

            var token = _lea.Subscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent() { Value = "one" });
            _lea.Unsubscribe<TestEvent>(token);
            _lea.Publish(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void UnsubscribeNotSubscribed()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Handler1(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
            }

            void Handler2(TestEvent2 evt)
            {
                handlerCalled++;
            }

            _lea.Subscribe<TestEvent>(Handler1);
            _lea.Unsubscribe<TestEvent2>(Handler2);
            _lea.Publish(new TestEvent() { Value = "one" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void UnsubscribeNotSubscribedAsyncHandler()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            async Task Handler1(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
                await Task.Delay(1);
            }

            async Task Handler2(TestEvent2 evt)
            {
                handlerCalled++;
                await Task.Delay(1);
            }

            _lea.Subscribe<TestEvent>(Handler1);
            _lea.Unsubscribe<TestEvent2>(Handler2);
            _lea.Publish(new TestEvent() { Value = "one" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void UnsubscribeNonGenericNotSubscribed()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Handler1(IEvent evt)
            {
                handlerCalled++;
                lastValue = ((TestEvent)evt).Value;
            }

            void Handler2(IEvent evt)
            {
                handlerCalled++;
            }

            _lea.Subscribe(typeof(TestEvent), Handler1);
            _lea.Unsubscribe(typeof(TestEvent2), Handler2);
            _lea.Publish(new TestEvent() { Value = "one" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void UnsubscribeNonGenericNotSubscribedAsyncHandler()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            async Task Handler1(IEvent evt)
            {
                handlerCalled++;
                lastValue = ((TestEvent)evt).Value;
                await Task.Delay(1);
            }

            async Task Handler2(IEvent evt)
            {
                handlerCalled++;
                await Task.Delay(1);
            }

            _lea.Subscribe(typeof(TestEvent), Handler1);
            _lea.Unsubscribe(typeof(TestEvent2), Handler2);
            _lea.Publish(new TestEvent() { Value = "one" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void UnsubscribeWithTokenNotSubscribed()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
            }

            var token = _lea.Subscribe<TestEvent>(Handler);
            _lea.Unsubscribe<TestEvent2>(token);
            _lea.Publish(new TestEvent() { Value = "one" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void UnsubscribeWithTokenNotSubscribedAsyncHandler()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            async Task Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
                await Task.Delay(1);
            }

            var token = _lea.Subscribe<TestEvent>(Handler);
            _lea.Unsubscribe<TestEvent2>(token);
            _lea.Publish(new TestEvent() { Value = "one" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void PublishNotSubscribed()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
            }

            _lea.Subscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent2());

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(0));
                Assert.That(lastValue, Is.Null);
            });
        }

        [Test]
        public void PublishNotSubscribedAsyncHandler()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            async Task Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
                await Task.Delay(1);
            }

            _lea.Subscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent2());

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(0));
                Assert.That(lastValue, Is.Null);
            });
        }

        [Test]
        public void HandlerCollectedSubscribe()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Callback(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
            }

            SetUpOutOfScopeHandler(Callback);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            _lea.Subscribe<TestEvent>(Callback);
            _lea.Publish(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(2));
                Assert.That(lastValue, Is.EqualTo("two"));
            });
        }

        [Test]
        public void HandlerCollectedSubscribeAsyncHandler()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Callback(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
            }

            SetUpOutOfScopeAsyncHandler(Callback);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            _lea.Subscribe<TestEvent>(Callback);
            _lea.Publish(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(2));
                Assert.That(lastValue, Is.EqualTo("two"));
            });
        }

        [Test]
        public void HandlerCollectedUnsubscribe()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Callback(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
            }

            SetUpOutOfScopeHandler(Callback);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            _lea.Subscribe<TestEvent>(Callback);
            _lea.Unsubscribe<TestEvent>(Callback);
            _lea.Publish(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void HandlerCollectedUnsubscribeAsyncHandler()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Callback(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
            }

            SetUpOutOfScopeAsyncHandler(Callback);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            _lea.Subscribe<TestEvent>(Callback);
            _lea.Unsubscribe<TestEvent>(Callback);
            _lea.Publish(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void HandlerCollectedPublish()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Callback(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
            }

            SetUpOutOfScopeHandler(Callback);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            _lea.Publish(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void HandlerCollectedPublishAsyncHandler()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Callback(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
            }

            SetUpOutOfScopeAsyncHandler(Callback);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            _lea.Publish(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void Dispose()
        {
            Assert.DoesNotThrow(() =>
            {
                using var lea = new EventAggregator();
                lea.Subscribe<TestEvent>(e => { });
            });
        }

        #region Threading

        [Test]
        public void BackgroundThread()
        {
            var handlerCalled = 0;
            string? lastValue = null;
            var publishThreadId = Environment.CurrentManagedThreadId;
            int handlerThreadId = publishThreadId;

            void Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
                handlerThreadId = Environment.CurrentManagedThreadId;
            }

            _lea.Subscribe<TestEvent>(Handler, SubscriberThread.BackgroundThread);
            _lea.Publish(new TestEvent() { Value = "one" });
            _lea.Unsubscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
                Assert.That(publishThreadId, Is.Not.EqualTo(handlerThreadId));
            });
        }

        [Test]
        public async Task BackgroundThreadAsync()
        {
            var handlerCalled = 0;
            string? lastValue = null;
            var publishThreadId = Environment.CurrentManagedThreadId;
            int handlerThreadId = publishThreadId;

            Task Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
                handlerThreadId = Environment.CurrentManagedThreadId;
                return Task.CompletedTask;
            }

            _lea.Subscribe<TestEvent>(Handler, SubscriberThread.BackgroundThread);
            await _lea.PublishAsync(new TestEvent() { Value = "one" });
            _lea.Unsubscribe<TestEvent>(Handler);
            await _lea.PublishAsync(new TestEvent() { Value = "two" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
                Assert.That(publishThreadId, Is.Not.EqualTo(handlerThreadId));
            });
        }

        [Test]
        public void ContextThread()
        {
            var context = new TestSynchronizationContext();
            var handlerCalled = 0;
            string? lastValue = null;
            var publishThreadId = Environment.CurrentManagedThreadId;
            int handlerThreadId = publishThreadId;

            void Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
                handlerThreadId = Environment.CurrentManagedThreadId;
            }

            _lea.SetSynchronizationContext(context);
            _lea.Subscribe<TestEvent>(Handler, SubscriberThread.ContextThread);
            _lea.Publish(new TestEvent() { Value = "one" });
            _lea.Unsubscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent() { Value = "two" });

            Task.Delay(100).Wait();

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
                Assert.That(publishThreadId, Is.Not.EqualTo(handlerThreadId));
            });
        }

        [Test]
        public async Task ContextThreadAsync()
        {
            var context = new TestSynchronizationContext();
            var handlerCalled = 0;
            string? lastValue = null;
            var publishThreadId = Environment.CurrentManagedThreadId;
            int handlerThreadId = publishThreadId;

            Task Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
                handlerThreadId = Environment.CurrentManagedThreadId;
                return Task.CompletedTask;
            }

            _lea.SetSynchronizationContext(context);
            _lea.Subscribe<TestEvent>(Handler, SubscriberThread.ContextThread);
            await _lea.PublishAsync(new TestEvent() { Value = "one" });
            _lea.Unsubscribe<TestEvent>(Handler);
            await _lea.PublishAsync(new TestEvent() { Value = "two" });

            await Task.Delay(100);

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
                Assert.That(publishThreadId, Is.Not.EqualTo(handlerThreadId));
            });
        }

        #endregion

        #region Failures

        [Test]
        public void PublishRecursive()
        {
            var loggerMock = new Mock<ILogger<IEventAggregator>>();
            loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _lea = new EventAggregator(loggerMock.Object);

            var handlerCalled = 0;
            string? lastValue = null;

            void Handler1(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;

                _lea.Publish(new TestEvent2());
            }

            void Handler2(TestEvent2 evt)
            {
                if (handlerCalled == 1)
                {
                    _lea.Publish(new TestEvent() { Value = "two" });
                }
            }

            _lea.Subscribe<TestEvent>(Handler1);
            _lea.Subscribe<TestEvent2>(Handler2);
            _lea.Publish(new TestEvent() { Value = "one" });

            // No errors should be logged
            loggerMock.Verify(logger => logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((@object, @type) => ((IReadOnlyList<KeyValuePair<string, object?>>)@object).Any(x => x.Value!.ToString() == "TestEvent")),
                    It.Is<Exception?>(x => x == null),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);

            loggerMock.Verify(logger => logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((@object, @type) => ((IReadOnlyList<KeyValuePair<string, object?>>)@object).Any(x => x.Value!.ToString() == "TestEvent2")),
                    It.Is<Exception?>(x => x == null),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Test]
        public void PublishRecursiveNoLogger()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Handler1(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;

                _lea.Publish(new TestEvent2());
            }

            void Handler2(TestEvent2 evt)
            {
                if (handlerCalled == 1)
                {
                    _lea.Publish(new TestEvent() { Value = "two" });
                }
            }

            _lea.Subscribe<TestEvent>(Handler1);
            _lea.Subscribe<TestEvent2>(Handler2);
            Assert.DoesNotThrow(() => _lea.Publish(new TestEvent() { Value = "one" }));
        }

        [Test]
        public void SubscribeNullHandler()
        {
            Assert.Throws<ArgumentNullException>(() => _lea.Subscribe<TestEvent>(null!));
        }

        [Test]
        public void SubscribeNullAsyncHandler()
        {
            Assert.Throws<ArgumentNullException>(() => _lea.Subscribe((AsyncEventAggregatorHandler<TestEvent>)null!));
        }

        [Test]
        public void UnsubscribeNullHandler()
        {
            Assert.Throws<ArgumentNullException>(() => _lea.Unsubscribe((EventAggregatorHandler<TestEvent>)null!));
        }

        [Test]
        public void UnsubscribeNullAsyncHandler()
        {
            Assert.Throws<ArgumentNullException>(() => _lea.Unsubscribe((AsyncEventAggregatorHandler<TestEvent>)null!));
        }

        [Test]
        public void PublishNull()
        {
            Assert.Throws<ArgumentNullException>(() => _lea.Publish(null!));
        }

        [Test]
        public void PublishAsyncNull()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _lea.PublishAsync(null!));
        }

        [Test]
        public void InvokeLogError()
        {
            var loggerMock = new Mock<ILogger<IEventAggregator>>();
            loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _lea = new EventAggregator(loggerMock.Object);

            var handlerCalled = 0;
            string? lastValue = null;

            void Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
            }

            void HandlerThrow(TestEvent evt)
            {
                throw new InvalidOperationException("Test");
            }

            async Task AsyncHandlerThrow(TestEvent evt)
            {
                await Task.Delay(1);
                throw new FileNotFoundException("Async Test");
            }

            _lea.Subscribe<TestEvent>(Handler);
            _lea.Subscribe<TestEvent>(HandlerThrow);
            _lea.Subscribe<TestEvent>(AsyncHandlerThrow);

            _lea.Publish(new TestEvent() { Value = "one" });

            loggerMock.Verify(
                logger => logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((@object, @type) => true),
                    It.Is<Exception>(x => x.InnerException is InvalidOperationException),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void InvokeErrorNoLogger()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
            }

            void HandlerThrow(TestEvent evt)
            {
                throw new InvalidOperationException("Test");
            }

            async Task AsyncHandlerThrow(TestEvent evt)
            {
                await Task.Delay(1);
                throw new ApplicationException("Async Test");
            }

            _lea.Subscribe<TestEvent>(Handler);
            _lea.Subscribe<TestEvent>(HandlerThrow);
            _lea.Subscribe<TestEvent>(AsyncHandlerThrow);

            _lea.Publish(new TestEvent() { Value = "one" });

            Assert.Multiple(() =>
            {
                Assert.That(handlerCalled, Is.EqualTo(1));
                Assert.That(lastValue, Is.EqualTo("one"));
            });
        }

        [Test]
        public void ContextThreadNoContext()
        {
            var loggerMock = new Mock<ILogger<IEventAggregator>>();
            loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _lea = new EventAggregator(loggerMock.Object);

            var publishThreadId = Environment.CurrentManagedThreadId;
            int handlerThreadId = publishThreadId;

            static void Handler(TestEvent evt)
            {
            }

            _lea.Subscribe<TestEvent>(Handler, SubscriberThread.ContextThread);
            _lea.Publish(new TestEvent() { Value = "one" });

            loggerMock.Verify(
                logger => logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((@object, @type) => true),
                    It.Is<Exception>(x => x is InvalidOperationException),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task ContextThreadAsyncNoContext()
        {
            var loggerMock = new Mock<ILogger<IEventAggregator>>();
            loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _lea = new EventAggregator(loggerMock.Object);

            var publishThreadId = Environment.CurrentManagedThreadId;
            int handlerThreadId = publishThreadId;

            static Task Handler(TestEvent evt)
            {
                return Task.CompletedTask;
            }

            _lea.Subscribe<TestEvent>(Handler, SubscriberThread.ContextThread);
            await _lea.PublishAsync(new TestEvent() { Value = "one" });

            loggerMock.Verify(
                logger => logger.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((@object, @type) => true),
                    It.Is<Exception>(x => x is InvalidOperationException),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public void SubscribeBadThreadEnum()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _lea.Subscribe<TestEvent>(t => { }, (SubscriberThread)42));
        }

        [Test]
        public void SubscribeAsyncBadThreadEnum()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _lea.Subscribe<TestEvent>(t => { return Task.CompletedTask; }, (SubscriberThread)42));
        }

        #endregion

        private void SetUpOutOfScopeHandler(EventAggregatorHandler<TestEvent> callback)
        {
            void Handler(TestEvent evt)
            {
                callback(evt);
            }

            _lea.Subscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent() { Value = "one" });
        }

        private void SetUpOutOfScopeAsyncHandler(EventAggregatorHandler<TestEvent> callback)
        {
            async Task Handler(TestEvent evt)
            {
                callback(evt);
                await Task.Delay(1);
            }

            _lea.Subscribe<TestEvent>(Handler);
            _lea.Publish(new TestEvent() { Value = "one" });
        }
    }

    [ExcludeFromCodeCoverage]
    public class TestEvent : IEvent
    {
        public string? Value { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class TestEvent2 : IEvent
    {
    }

    [ExcludeFromCodeCoverage]
    public class TestSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            Task.Run(() => d(state));
        }
    }
}