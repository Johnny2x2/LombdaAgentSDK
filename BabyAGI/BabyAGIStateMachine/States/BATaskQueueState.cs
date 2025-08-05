using BabyAGI.BabyAGIStateMachine.DataModels;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.AgentStateSystem;


namespace BabyAGI.BabyAGIStateMachine.States
{
    public class BATaskQueueState : BaseState<List<QueueTask>, QueueTask>
    {
        public BATaskQueueState(StateMachine stateMachine) 
        {
        }

        //public override Agent InitilizeStateAgent() => Agent.DummyAgent(); // Unused in this state, but required by the base class

        public override async Task<QueueTask> Invoke(List<QueueTask> inputs)
        {
            var currentTask = new QueueTask();
            var tasksForEval = new List<QueueTask>();
            //"queueTasksForEval"
            if (!CurrentStateMachine!.RuntimeProperties.ContainsKey("queueTasksForEval"))
            {
                if (!CurrentStateMachine.RuntimeProperties.TryAdd("queueTasksForEval", new Queue<QueueTask>()))
                {
                    throw new InvalidOperationException("Failed to initialize \"queueTasksForEval\" in the state machine runtime properties.");
                }
            }
            else
            {
                if ((CurrentStateMachine.RuntimeProperties.TryGetValue("queueTasksForEval", out object evalQueue)))
                {
                    tasksForEval = ((List<QueueTask>)evalQueue);
                }

            }
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
                    foreach (var task in inputs)
                    {
                        ((Queue<QueueTask>)queue).Enqueue(task);
                    }

                    if(((Queue<QueueTask>)queue).Count > 0)
                    {
                        currentTask = ((Queue<QueueTask>)queue).Dequeue();
                        tasksForEval.Add(currentTask);
                        CurrentStateMachine.RuntimeProperties.AddOrUpdate("queueTasksForEval", tasksForEval, (key, val) => tasksForEval);
                    } 
                }   
            }

            return currentTask;
        }
    }
}
