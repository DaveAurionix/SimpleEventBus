Feature: Event publishing and subscription

Scenario: Event subscriptions receive published events
	Given an endpoint has subscribed to events
	When an event is published
	Then the endpoint receives the event