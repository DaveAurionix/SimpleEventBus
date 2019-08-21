using GherkinSpec.Logging;
using GherkinSpec.TestModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleEventBus.Testing;
using SimpleEventBus.Testing.StepDefinitions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SimpleEventBus.FileTransport.IntegrationTests.Configuration
{
    [Steps]
    public static class Startup
    {
        private static Endpoint endpoint;
        private static string messagesStoragePath;

        [BeforeRun]
        public static async Task Setup(TestRunContext testRunContext)
        {
            var services = new ServiceCollection();

            messagesStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "messages");

            testRunContext.EventualSuccess.MaximumAttempts = 5;

            testRunContext.ServiceProvider = services
                .AddScoped<TestData>()
                .AddAllStepsClassesAsScoped()
                .AddAllStepsClassesAsScoped(typeof(TestData).Assembly)
                .AddLogging(
                    builder => builder
                        .AddConsole()
                        .AddTestLogging(testRunContext.Logger))
                .AddSimpleEventBus(
                    options => options
                        .UseEndpointName(typeof(Startup).Assembly.GetName().Name)
                        .UseFileBus(messagesStoragePath)
                        .UseSingletonHandlersIn(typeof(TestData).Assembly)
                        .RouteCommandToSelf<TestCommand>()
                        .UseRetries(retryOptions =>
                        {
                            retryOptions.DeferredRetryInterval = TimeSpan.FromSeconds(6);
                        }))
                .BuildServiceProvider();

            endpoint = testRunContext.ServiceProvider.GetRequiredService<Endpoint>();
            await endpoint
                .StartListening()
                .ConfigureAwait(false);
        }

        [AfterRun]
        public static async Task Teardown(TestRunContext testRunContext)
        {
            if (endpoint != null)
            {
                await endpoint
                    .ShutDown()
                    .ConfigureAwait(false);
            }

            if (Directory.Exists(messagesStoragePath))
            {
                Directory.Delete(messagesStoragePath, true);
            }

            var typedProvider = (ServiceProvider)testRunContext.ServiceProvider;
            typedProvider.Dispose();
        }
    }
}
