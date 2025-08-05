using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Examples.Basic
{
    internal class BasicStructuredOutputsEnums
    {
        [Test]
        public async Task RunHelloWorld()
        {
            LLMTornadoModelProvider client =
                new(ChatModel.OpenAi.Gpt41.V41Mini, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], true, enableWebSearch:true);

            Agent agent = new Agent(client, "Assistant", "Have fun", _output_schema:typeof(conditions));

            RunResult result = await Runner.RunAsync(agent, "What is the weather in boston?");

            conditions conditions = result.ParseJson<conditions>();

            Console.WriteLine($"[CONDITIONS]: {conditions.summary}");
            Console.WriteLine($"[CONDITIONS]: {conditions.weather}");
        }

        public struct conditions
        {
            public string summary { get; set; }

            [JsonConverter(typeof(JsonStringEnumConverter))]
            public condition weather { get; set; }
        }

        public enum condition
        {
            Sunny,
            Cloudy,
            Rainy,
            Snowy,
            Windy
        }
    }
}
