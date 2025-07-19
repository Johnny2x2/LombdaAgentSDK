using BabyAGI.BabyAGIStateMachine.DataModels;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;

namespace BabyAGI.BabyAGIStateMachine.States
{
    public class BATaskQueueState : BaseState<TaskBreakdownResult, QueueTask>
    {
        public BATaskQueueState(StateMachine stateMachine) 
        {
        }

        //public override Agent InitilizeStateAgent() => Agent.DummyAgent(); // Unused in this state, but required by the base class

        public override async Task<QueueTask> Invoke(TaskBreakdownResult input)
        {
            var currentTask = new QueueTask();

            if (!CurrentStateMachine!.RuntimeProperties.ContainsKey("TaskQueue"))
            {
                if(!CurrentStateMachine.RuntimeProperties.TryAdd("TaskQueue", new Queue<QueueTask>()))
                {
                    throw new InvalidOperationException("Failed to initialize TaskQueue in the state machine runtime properties.");
                }
            }
            else
            {
                if((CurrentStateMachine.RuntimeProperties.TryGetValue("TaskQueue", out object queue)))
                {
                    foreach (var task in input.Tasks)
                    {
                        ((Queue<QueueTask>)queue).Enqueue(new QueueTask(task.ToString()));
                    }
                    
                    currentTask = ((Queue<QueueTask>)queue).Dequeue();
                }
                
            }

            return currentTask;
        }
    }
}
