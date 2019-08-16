using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimpleEventBus.Abstractions.Incoming;
using SimpleEventBus.Incoming;
using SimpleEventBus.Outgoing;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEventBus.UnitTests.Incoming
{
    [TestClass]
    public class HandlerInvokerShould
    {
        private HandlerInvoker behaviour;
        private readonly Mock<IMessageSource> mockMessageSource = new Mock<IMessageSource>();
        private readonly MockHandler mockHandler = new MockHandler();
        private IServiceProvider serviceProvider;
        private string exampleEventMappedTypeName;

        [TestInitialize]
        public async Task Setup()
        {
            exampleEventMappedTypeName = FullNameTypeMap.Instance.GetNameForType(typeof(ExampleEvent));

            var services = new ServiceCollection();
            services.AddSingleton(mockHandler);
            serviceProvider = services.BuildServiceProvider();

            behaviour = new HandlerInvoker(
                mockMessageSource.Object,
                services,
                FullNameTypeMap.Instance,
                "EndpointName",
                Mock.Of<IOutgoingPipeline>());

            await behaviour
                .Initialise(CancellationToken.None)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public void RegisterHandlersFromIoCContainerWithMessageSourceWhenInitialised()
        {
            mockMessageSource.Verify(
                m => m.EnsureSubscribed(
                    It.Is<SubscriptionDescription>(
                        description => description.MessageTypeNames.Count() == 1
                            && description.MessageTypeNames.First() == exampleEventMappedTypeName),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task InvokeHandlersForMessage()
        {
            using (var serviceScope = serviceProvider.CreateScope())
            {
                var message = IncomingMessageBuilder.BuildWithBody(new ExampleEvent());

                await behaviour
                    .Process(
                        message,
                        new Context(serviceScope))
                    .ConfigureAwait(false);

                Assert.IsNotNull(mockHandler.CalledWith);
                Assert.AreSame(message.Body, mockHandler.CalledWith);
            }
        }

        [TestMethod]
        public async Task NotWrapExceptionsInHandlersWithAnInvocationException()
        {
            using (var serviceScope = serviceProvider.CreateScope())
            {
                var message = IncomingMessageBuilder.BuildWithBody(new ExampleEvent());

                mockHandler.ThrowException = true;

                var exception = await Assert
                    .ThrowsExceptionAsync<InvalidOperationException>(
                        () => behaviour
                            .Process(
                                message,
                                new Context(serviceScope)))
                    .ConfigureAwait(false);

                Assert.AreEqual("Was told to throw.", exception.Message);
            }
        }
    }
}
