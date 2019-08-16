Feature: Immediate and deferred retries

Scenario: Failing messages are retried
	Given an endpoint has subscribed to events causing an exception
	When an event causing an exception is published
	Then the endpoint receives the event several times immediately according to the retry settings
	And the endpoint receives the event several times eventually according to the retry settings