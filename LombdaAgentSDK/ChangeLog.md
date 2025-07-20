# New
* Added in Very bad user interface for the agent debugging on windows
	* allow verbose logging right side and streaming chat on the left side
* Added in IsDeadEnd To State because it needed to stop reruning "failed" state
* Added in AgentStateMachine this will allow us to have a state machine for the agent
	* BaseStates and AgentStates can be used to create a state machine
* Added in AgentState a state made for agent execution
* Added in LombdaAgent the uniter between Agent and StateMachine
* Added in Events for logging and debugging

# Fixed
* Fixed the structured outputs to use Enum, but above enum field requires `[JsonConverter(typeof(JsonStringEnumConverter))]`
* Fixed the state machine with exiting and finishing when being set from inside the state machine

# Known Issues
* Descriptions on fields in structured outputs messes up the deserialization.. removing that feature for now