Feature: Event publishing and subscription

Scenario: Event subscriptions receive published events via a secondary bus if primary bus is faulty
	Given an endpoint has subscribed to events for a failover test
	When a failover test event is published
	Then the endpoint receives the failover test event