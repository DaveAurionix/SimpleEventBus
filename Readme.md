# SimpleEventBus

## Project status

Warning!  This is not yet considered to be production-ready.  API surface is evolving, and performance testing has not been performed.  As for documentation ... umm ... it isn't yet up to our usual standard yet ...  here be many, many dragons.

## Overview

TODO

## Getting started

TODO - Reference example repositories

## Development and testing

This project uses a Visual Studio 2019 solution.  After checking the project out and opening the solution, no special steps are needed to build it or to run the unit test projects.

Integration test projects usually run with no extra configuration.  The only exception is the AzureServiceBusTransport.IntegrationTests project.  This currently needs a new appsettings.Development.json file created alongside appsettings.json with a connection string in pointing to an Azure Service Bus instance that you have created for development and testing purposes.  Example file contents:

```json
"AzureServiceBusTransport": {
    "ConnectionStrings": [
      "Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<keyname>;SharedAccessKey=<key>"
    ],
```

Copy the actual connection string from Azure Portal for your Azure Service Bus instance.  **Do not check that connection string into source control**.

## TODO

### MVP (v0.1)

* Example repos

* Search code for TODO comments
  * Unit tests for dequeue count in file and in-memory buses
* Logging (separate extensions repo?)
* Monitoring (separate extensions repo for AppInsights)
* Circuit breaker (separate extensions repo?) - investigate Polly, e.g. https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/implement-circuit-breaker-pattern
* Performance tests
* Time-delayed (secheduled events and commands)
* Pushing a batch of messages that is too big for ASB, or rely on ASB's internal batching

* Experimental hookup with EventSourcing package
  * Handler can capture headers such as correlationid and domainundertest for saving to the event stream
  * Use new https://devblogs.microsoft.com/aspnet/dotnet-core-workers-in-azure-container-instances/?utm_source=vs_developer_news&utm_medium=referral BackgroundService pattern?
  * Publisher can re-append headers such as correlationid and domainundertest when publishing from the dispatcher

* Finish WIP - multiple handlers for the same event with a message header (subscriber re-publishes with attached SpecificEndpoint so only self picks up the new message, but also SpecificHandler - once for each handler class)
  * If multiple handlers exist then subscriber republishes the messages with a new header
  * If receiving a message with that header, dispatch to that exact handler.
  * Check headers still flow 

### Cautious go-live (v0.5)

* Contribution license
* Documentation (see notes below)

### V1

* SourceLink support
* Polymorphic subscription (v1 - non-abstract base classes)
* Support custom message headers/metadata
* Case insensitive message headers due to PHP ASB libraries
* Support multiple transports - configure per-event / per-command transports (bridge example?)
* When abandoning ASB message, set the reason or exception in "properties to modify" parameter?

### vNext

* Rate limiter?
  * Options has UseRateLimit(maximumMessages:500, enforcedInPeriod:TimeSpan.FromSeconds(1))
  * Behaviour enforces rate limit
* Priority queues concept (may just be documenting a good pattern)

### Doc comments

#### Feature highlights (future intent)

* Open source
* Publish and subscribe to events (and send commands)
* Support failover with cheap waiting (long request delays yet fail-fast on faulty connection)
* Polymorphic subscription
* Immediate and deferred retries
* Multiple handlers for the same event
* (service test subscription rule version not fixed at v1_15_0_0)
* In-memory and file-based transports for in-process and cross-process tests
* Azure Service Bus transport with enhanced resilience
* Dynamic batch size (adjusts automatically to auto-tune for optimum performance)
* Concurrent processing limit independent of message retrieval (positive and negative feedback to tune the batch size to minimise cost and network usage whilst maintaining desired limit on number of messages processed in parallel)
* DomainUnderTest - for automated testing using production code footprint
* Flow correlation id automatically across HTTP calls and exchanged messages
* Command routing configured away from application code

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
* Complex publisher and subscriber
* Web API and publisher
* Web API and background task subscriber?

#### ASP.NET Core Extensions

#### Misc

* Type map - purpose, configuration examples
* Setting endpoint name - purpose, config examples

## Future Ideas

* Support tool to easily publish specific messages (JSON?)
* Auditing that builds up data of endpoints and records what events/commands they publish/send and what endpoint receives them - visualise - but what is the use case?
