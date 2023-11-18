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
        public void HappyPath()
        {
            var handlerCalled = 0;
            string? lastValue = null;

            void Handler(TestEvent evt)
            {
                handlerCalled++;
                lastValue = evt.Value;
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
        public void HappyPathAsyncHandler()
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

        #region Failures

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
        public void InvokeLogError()
        {
            var loggerMock = new Mock<ILogger<IEventAggregator>>();
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

            //loggerMock.Verify(
            //    logger => logger.Log(
            //        It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
            //        It.IsAny<EventId>(),
            //        It.IsAny<It.IsAnyType>(),
            //        It.Is<Exception>(x => x is FileNotFoundException),
            //        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            //    Times.Once);

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
}