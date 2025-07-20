using BabyAGI.BabyAGIStateMachine.DataModels;
using BabyAGI.BabyAGIStateMachine.States;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;

namespace BabyAGI.BabyAGIStateMachine
{
    public class BabyAgiStateMachine : AgentStateMachine<string, ProgressReport>
    {
        public BabyAgiStateMachine(LombdaAgent lombdaAgent) : base(lombdaAgent)
        {
            // Initialize the runtime properties for the state machine
            RuntimeProperties.TryAdd("CurrentGoal", string.Empty);
            RuntimeProperties.TryAdd("queueTasksForEval", new List<QueueTask>());
            RuntimeProperties.TryAdd("TaskQueue", new Queue<QueueTask>());
        }

        public override void InitilizeStates()
        {
            BAEntryTaskCreationState entryTaskCreationState = new BAEntryTaskCreationState(this); //This will create the initial tasks based on the user input
            BATaskQueueState taskQueueState = new BATaskQueueState(this);//This will queue tasks for execution
            BAExecutionState executionState = new BAExecutionState(this);//This will execute the tasks in the queue
            TaskDetailEnrichmentState taskDetailEnrichmentState = new TaskDetailEnrichmentState(this) { AllowsParallelTransitions = true }; //This will enrich the task results with additional information
            AddToMemoryState addToMemoryState = new AddToMemoryState(this) { IsDeadEnd = true }; //This will store the enriched data in long-term memory
            AddToShortTermMemory addToShortTermMemory = new AddToShortTermMemory(this) { IsDeadEnd = true }; //This will store the enriched data in short-term memory
            BAProgressManager progressManager = new BAProgressManager(this); // This will manage the progress of the tasks and their execution states
            BATaskGeneratorState taskGeneratorState = new BATaskGeneratorState(this); // This will generate new tasks based on the current state of the system

            //Enter the state machine and start with the entry task creation state
            entryTaskCreationState.AddTransition(taskQueueState); //Happens only once at the start

            //Passthru to execution after the task queue is populated
            taskQueueState.AddTransition(executionState);

            //Execution state will send the data to be enriched before moving to the next state
            executionState.AddTransition(taskDetailEnrichmentState);

            //Happen In parallel
            //Store the enriched data in long-term memory if the enrichment state deems it useful
            taskDetailEnrichmentState.AddTransition(
                result=>result.StoreInLongTermMemory, 
                (toConvert)=> new MemoryItem() { ToEmbed=toConvert.SummaryToEmbed,SaveSummary=toConvert.SummaryToRetrieve,UsefulMetadata=toConvert.UsefulMetadata},
                addToMemoryState);
            //Store the enriched data in short-term memory
            taskDetailEnrichmentState.AddTransition(
                (toConvert) => new MemoryItem() { ToEmbed = toConvert.SummaryToEmbed, SaveSummary = toConvert.SummaryToRetrieve, UsefulMetadata = toConvert.UsefulMetadata },
                addToShortTermMemory);
            //Move to the progress manager to handle the task result
            taskDetailEnrichmentState.AddTransition((toConvert) => new QueueTask(toConvert.SummaryToRetrieve), progressManager);

            //Progress manager deamed the task completed and will exit the state machine
            progressManager.AddTransition((result) => result.Status.Status == ProgressState.Completed, new ExitState()); //Only Exit
            //Progress manager not satisfied with the task result and will generate new tasks
            progressManager.AddTransition((result) => result.Status.Status == ProgressState.Progressing && IsTaskQueueEmpty(), (toConvert) => toConvert.ToString(), taskGeneratorState);
            //Moving forward to the next task
            progressManager.AddTransition((result) => result.Status.Status == ProgressState.Progressing && !IsTaskQueueEmpty(), (toConvert)=>new List<QueueTask>(), taskQueueState);
            //Stagnating or regression, will try to generate new tasks
            progressManager.AddTransition((result) => result.StallCount > 2,(toConvert) => toConvert.ToString(), taskGeneratorState);
            //Progress manager not satisfied with the task result and will generate new tasks
            progressManager.AddTransition(_ => IsTaskQueueEmpty(), (toConvert) => toConvert.ToString(), taskGeneratorState);
            //Default transition for stagnating or regression before stalling
            progressManager.AddTransition((toConvert) => new List<QueueTask>(), taskQueueState);
            
            //Task generate will send new tasks to the task queue state
            taskGeneratorState.AddTransition(taskQueueState);

            SetEntryState(entryTaskCreationState);
            SetOutputState(progressManager);
        }

        public bool IsTaskQueueEmpty()
        {
            if (RuntimeProperties.TryGetValue("TaskQueue", out object queue))
            {
                return ((Queue<QueueTask>)queue).Count == 0;
            }
            return true; // If the TaskQueue is not initialized, consider it empty
        }
    }
}
