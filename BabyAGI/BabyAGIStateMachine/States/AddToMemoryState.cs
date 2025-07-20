using BabyAGI.BabyAGIStateMachine.DataModels;
using BabyAGI.BabyAGIStateMachine.Memory;
using LlmTornado.Moderation;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.BabyAGIStateMachine.States
{
    public class AddToMemoryState : BaseState<MemoryItem, MemoryItem>
    {
        public AddToMemoryState(StateMachine stateMachine)
        {
            CurrentStateMachine = stateMachine;
        }

        public override async Task<MemoryItem> Invoke(MemoryItem input)
        {
            await BabyAGIMemory.AddTaskResultToLongTermMemory(input.ToEmbed, input.SaveSummary, input.UsefulMetadata);
            return input;
        }
    }
}
