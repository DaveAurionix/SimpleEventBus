Feature: Correlation-ID flows

Scenario: Correlation-ID header flows from incoming messages to outgoing messages
	Given an endpoint has subscribed to events
	When an event is published
	Then the endpoint receives the event with the correct Correlation-ID

Scenario: A new correlation-ID header is added to outgoing messages if missing from the incoming messages
	Given an endpoint has subscribed to events
	When an event is published without a Correlation-ID
	Then the endpoint receives the event with any Correlation-ID

Scenario: Multiple handlers for the same event all receive the same Correlation-ID
	Given an endpoint has subscribed to the same event multiple times
	When an event suitable for testing multiple handlers is published
	Then all handlers receive the event with the same Correlation-ID