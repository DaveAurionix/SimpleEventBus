using GherkinSpec.Logging;
using GherkinSpec.TestModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleEventBus.Testing;
using SimpleEventBus.Testing.StepDefinitions;
using System;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport.IntegrationTests.Configuration
{
    [Steps]
    public static class Startup
    {
        private static Endpoint endpoint;

        [BeforeRun]
        public static async Task Setup(TestRunContext testRunContext)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile(@"Configuration/appsettings.json", optional: false)
                .AddJsonFile($@"Configuration/appsettings.Development.json", optional: true)
                .AddEnvironmentVariables();
            var settings = configurationBuilder.Build().Get<Settings>();

            var services = new ServiceCollection();

            testRunContext.EventualSuccess.MaximumAttempts = 5;

            testRunContext.ServiceProvider = services
                .AddScoped<TestData>()
                .AddSingleton(settings)
                .AddAllStepsClassesAsScoped()
                .AddAllStepsClassesAsScoped(typeof(TestData).Assembly)
                .AddSingleton(testRunContext.Logger)
                .AddLogging(
                    builder => builder
                        .AddConsole()
                        .AddTestLogging(testRunContext.Logger))
                .AddSimpleEventBus(
                    options => options
                        .UseEndpointName("SimpleEventBus.AzureServiceBusTransport.Tests")
                        .UseAzureServiceBus(settings.AzureServiceBusTransport)
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
