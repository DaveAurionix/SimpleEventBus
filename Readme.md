# SimpleEventBus

## Project status

Warning!  This is not yet considered to be production-ready.  API surface is evolving, and performance testing has not been performed.  As for documentation ... umm ... it isn't yet up to our usual standard yet ...  here be many, many dragons.

## Overview

A .NET Standard library that glues message-handler classes up to an Azure Service Bus instance. It's designed to minimise the amount of code required in applications in order to subscribe to and handle a message.

### Feature highlights

* Open source
* Publish and subscribe to events (and send commands)
* Support failover with cheap waiting (long request delays yet fail-fast on faulty connection)
* Immediate and deferred retries
* Multiple handlers for the same event
* In-memory and file-based transports for in-process and cross-process tests
* Azure Service Bus transport with enhanced resilience
* Dynamic batch size (adjusts automatically to auto-tune for optimum performance for a given desired number of concurrent messages)
* Flow correlation id automatically across HTTP calls and exchanged messages
* Command routing configured away from application code

These are the baseline features. More are available in [the Extensions package](https://github.com/GivePenny/SimpleEventBus.Extensions).

## Getting started

To get started using SimpleEventBus in your own development projects as an event-publisher or subscriber, see the [minimal example](https://github.com/GivePenny/SimpleEventBus.MinimalExample).

To get started developing and testing this repository, see the next section.

### Development and testing

This project uses a Visual Studio 2019 solution.  After checking the project out and opening the solution, no special steps are needed to build it or to run the unit test projects.

Integration test projects usually run with no extra configuration.  The one exception is the AzureServiceBusTransport.IntegrationTests project.  This currently needs a new appsettings.Development.json file created alongside appsettings.json with a connection string in it that points to an Azure Service Bus instance that you have created for development and testing purposes.  Example file contents (the MachineName allows multiple developers to share a Bus but on isolated Topics):

```json
"AzureServiceBusTransport": {
    "ConnectionStrings": [
      "Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<keyname>;SharedAccessKey=<key>"
    ],
    "TopicName": "{MachineName}-Test",
    "EnablePartitioning": false
```

Copy the actual connection string from Azure Portal for your Azure Service Bus instance.  **Do not check that connection string into source control**.

## TODO

### MVP (v0.1)

* Remove dependency on Newtonsoft.JSON
* Update examples repo (and to .NET Core 3.0 hostbuilder pattern)

* Fix time-dependent (therefore: flaky) unit tests in AzureServiceBusTransport.UnitTests
* Search code for TODO comments
  * Unit tests for dequeue count in file and in-memory buses
* Monitoring (separate extensions repo for AppInsights)
* Circuit breaker (separate extensions repo?) - investigate Polly, e.g. https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/implement-circuit-breaker-pattern
* Performance tests
* Time-delayed (secheduled events and commands)
* Pushing a batch of messages that is too big for ASB, or rely on ASB's internal batching
* PAT compatibility shim (separate repo)

* Experimental hookup with EventSourcing package
  * Handler can capture headers such as correlationid and domainundertest for saving to the event stream
  * Use new https://devblogs.microsoft.com/aspnet/dotnet-core-workers-in-azure-container-instances/?utm_source=vs_developer_news&utm_medium=referral BackgroundService pattern?
  * Publisher can re-append headers such as correlationid and domainundertest when publishing from the dispatcher

### Cautious go-live (v0.5)

* Contribution license
* Documentation (see notes below)

### V1

* SourceLink support
* Polymorphic subscription (v1 - non-abstract base classes)
* Support custom message headers/metadata
* Case insensitive message headers due to PHP ASB libraries
* When abandoning ASB message, set the reason or exception in "properties to modify" parameter?

### vNext

* Rate limiter?
  * Options has UseRateLimit(maximumMessages:500, enforcedInPeriod:TimeSpan.FromSeconds(1))
  * Behaviour enforces rate limit
* Priority queues concept (may just be documenting a good pattern)
* Support multiple transports - configure per-event / per-command transports (bridge example?)

### Doc comments

#### Feature highlights (future intent)

* Polymorphic subscription
* (service test subscription rule version not fixed at v1_15_0_0)

#### Handlers

* Lifetime can be Singleton, Scoped or Transient.  Latter two are equivalent and mean one instance per message.
* Show best-practice cancellationtoken mechanism for endpoints
* Warn about multiple handlers in the same subscriber (require enabling this with a config option, or automagically split to events with a specific endpoint AND a specific handler class - separate behaviour)

#### Transports

* In-memory transport: what it is, why it is, how/when to use it
* File transport
* ASB transport: config example, rule creation (last endpoint to start up is assumed to be latest version); resilience & failover strategies and behaviour

#### Concepts / code samples

* Endpoint (including lifetime methods) and MessagePublisher (rename?) - ALWAYS GET AN ENDPOINT INSTANCE AND CALL SHUTDOWN (even if only using MessagePublisher)
* Events versus commands
* Publishing and subscribing to events
* Sending and receiving commands
* Performance: concurrent fetching, concurrency limits, dynamic batch size
* DomainUnderTest & OperationCorrelationId - mention UseAspNetCoreRequestHeaderFlow and Correlation-ID and the MessageHeaderFlowHttpMessageHandler
* Making web calls from within message handlers: use HttpClientFactory (https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests) and .AddHttpMessageHandler() to plug in header flow (similar to https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/implement-circuit-breaker-pattern).

#### Example solutions

* Simple publisher and subscriber
* Publisher and subscriber with tests (unit, service)
* Web API and subscriber (with unit, service tests)

#### ASP.NET Core Extensions

#### Misc

* Type map - purpose, configuration examples
* Setting endpoint name - purpose, config examples

## Future Ideas

* Support tool to easily publish specific messages (JSON?)
* Auditing that builds up data of endpoints and records what events/commands they publish/send and what endpoint receives them - visualise - but what is the use case?
