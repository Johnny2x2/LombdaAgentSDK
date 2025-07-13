using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using NUnit.Framework;
using static Examples.Basic.BasicGuardRailExample;

namespace Examples.LlmTornado
{

    public class LTBasicGuardRailExample
    {
        [Test]
        public async Task Run()
        {
            await TestPassingGuardRail();

            // Act & Assert
            Assert.ThrowsAsync<GuardRailTriggerException>(() => TestFailingGuardRail());
        }

        public async Task TestPassingGuardRail()
        {
            LLMTornadoModelProvider client =
              new(ChatModel.OpenAi.Gpt41.V41Mini, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], true);

            Agent agent = new Agent(
                client,
                "Assistant",
                "You are a useful assistant.");

            RunResult result = await Runner.RunAsync(agent, "What is 2+2?", guard_rail: MathGuardRail);

            Console.WriteLine(result.Text);
        }

        async Task TestFailingGuardRail()
        {
            LLMTornadoModelProvider client =
              new(ChatModel.OpenAi.Gpt41.V41Mini, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], true);

            Agent agent = new Agent(
                client,
                "Assistant",
                "You are a useful assistant.");

            RunResult result = await Runner.RunAsync(agent, "What is the weather in boston?", guard_rail: MathGuardRail);

            Console.WriteLine(result.Text);
        }

        public struct IsMath
        {
            public string Reasoning {  get; set; }
            public bool is_math_request {  get; set; }
        }

        public async Task<GuardRailFunctionOutput> MathGuardRail(string input = "")
        {
            string oInfo = "";
            bool trigger = false;

            if (string.IsNullOrWhiteSpace(input)) return new GuardRailFunctionOutput(oInfo, trigger);
            LLMTornadoModelProvider client =
              new(ChatModel.OpenAi.Gpt41.V41Mini, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], true);

            Agent weather_guardrail = new Agent(
                client,
                "math_input_guardrail",
                "Check if the user is asking you a Math related question.", 
                _output_schema: typeof(IsMath));

            RunResult result = await Runner.RunAsync(weather_guardrail, input, single_turn:true);

            IsMath? isMath = result.ParseJson<IsMath>();

            return new GuardRailFunctionOutput(isMath?.Reasoning ?? "", !isMath?.is_math_request ?? false);
        }
    }
}
