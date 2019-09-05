Feature: Event publishing and subscription

Scenario: Event subscriptions receive published events
	Given an endpoint has subscribed to events
	When an event is published
	Then the endpoint receives the event

Scenario: Multiple handlers for the same event all receive a copy
	Given an endpoint has subscribed to the same event multiple times
	When an event suitable for testing multiple handlers is published
	Then all handlers receive the event