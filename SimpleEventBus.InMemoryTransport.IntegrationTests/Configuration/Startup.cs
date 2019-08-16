using GherkinSpec.TestModel;
using Microsoft.Extensions.DependencyInjection;
using SimpleEventBus.Testing;
using SimpleEventBus.Testing.StepDefinitions;
using System;
using System.Threading.Tasks;

namespace SimpleEventBus.InMemoryTransport.IntegrationTests.Configuration
{
    [Steps]
    public static class Startup
    {
        private static Endpoint endpoint;

        [BeforeRun]
        public static async Task Setup(TestRunContext testRunContext)
        {
            var services = new ServiceCollection();

            testRunContext.EventualSuccess.MaximumAttempts = 5;
            testRunContext.ServiceProvider = services
                .AddScoped<TestData>()
                .AddAllStepsClassesAsScoped()
                .AddAllStepsClassesAsScoped(typeof(TestData).Assembly)
                .AddSimpleEventBus(
                    options => options
                        .UseEndpointName(typeof(Startup).Assembly.GetName().Name)
                        .UseInMemoryBus()
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

            var typedProvider = (ServiceProvider)testRunContext.ServiceProvider;
            typedProvider.Dispose();
        }
    }
}
