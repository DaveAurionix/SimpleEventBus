Feature: Command sending and receiving

Scenario: Command handlers receive sent commands
	Given an endpoint has registered that it can handle commands
	When a command is sent
	Then the endpoint receives the command