using BabyAGI.BabyAGIStateMachine.States;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.Agents.ProjectCodingAgent.states
{
    public class TaskManagerState : BaseState<TaskBreakdownResult, TaskItem>
    {
        public override async Task<TaskItem> Invoke(TaskBreakdownResult input)
        {
            if (!CurrentStateMachine!.RuntimeProperties.ContainsKey("TaskQueue"))
            {
                if (!CurrentStateMachine.RuntimeProperties.TryAdd("TaskQueue", new Queue<TaskItem>()))
                {
                    throw new InvalidOperationException("Failed to initialize TaskQueue in the state machine runtime properties.");
                }
            }
            
            if(CurrentStateMachine.RuntimeProperties.TryGetValue("TaskQueue", out object? Tasks))
            {
                if (Tasks is not Queue<TaskItem>)
                {
                    throw new InvalidOperationException("TaskQueue in the state machine runtime properties is not of type Queue<TaskItem>.");
                }

                foreach (var task in input.Tasks)
                {
                    ((Queue<TaskItem>)Tasks).Enqueue(task);
                }

                if (((Queue<TaskItem>)Tasks).Count > 0)
                {
                    return ((Queue<TaskItem>)Tasks).Dequeue();
                }
            }

            return new TaskItem
            {
                TaskId = 0,
                Description = "No tasks available",
                ExpectedOutcome = "N/A",
                Dependencies = Array.Empty<string>(),
                Complexity = "Low",
                SuccessCriteria = "N/A"
            };
        }
    }
}
