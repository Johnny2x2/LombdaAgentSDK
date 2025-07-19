using BabyAGI.BabyAGIStateMachine.DataModels;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.BabyAGIStateMachine.States
{
    public class BAExecutionState : AgentState<string, QueueTask>
    {
        public override async Task<QueueTask> Invoke(string input)
        {
            var currentTask = new QueueTask();

            if (!CurrentStateMachine!.RuntimeProperties.ContainsKey("TaskQueue"))
            {
                if (!CurrentStateMachine.RuntimeProperties.TryAdd("TaskQueue", new Queue<QueueTask>()))
                {
                    throw new InvalidOperationException("Failed to initialize TaskQueue in the state machine runtime properties.");
                }
            }
            else
            {
                ((Queue<string>)CurrentStateMachine.RuntimeProperties["TaskQueue"]).Enqueue(input);
                currentTask = ((Queue<QueueTask>)CurrentStateMachine.RuntimeProperties["TaskQueue"]).Dequeue();
            }

            return currentTask;
        }
    }
   
}
