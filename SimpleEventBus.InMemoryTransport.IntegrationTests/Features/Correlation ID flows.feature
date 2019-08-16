Feature: Correlation-ID flows

Background:
	Given an endpoint has subscribed to events

Scenario: Correlation-ID header flows from incoming messages to outgoing messages
	When an event is published
	Then the endpoint receives the event with the correct Correlation-ID

Scenario: A new correlation-ID header is added to outgoing messages if missing from the incoming messages
	When an event is published without a Correlation-ID
	Then the endpoint receives the event with any Correlation-ID
