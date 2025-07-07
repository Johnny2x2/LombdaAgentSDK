using NUnit.Framework;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using System.Text.Json;
using LombdaAgentSDK;

namespace Examples.Basic
{
    internal class BasicStructuredOutputsExample
    {
        [Test]
        public async Task RunBasicStructuredOutputExample()
        {
            OpenAIModelClient client = new OpenAIModelClient("gpt-4o-mini");

            Agent agent = new Agent(
                new OpenAIModelClient("gpt-4o-mini"),
                "Assistant", 
                "Have fun",
                _output_schema: typeof(math_reasoning));

            RunResult result = await Runner.RunAsync(agent, "How can I solve 8x + 7 = -23?");

            //The easy way
            //Helper function to avoid doing the hard way
            math_reasoning mathResult = result.ParseJson<math_reasoning>();
            mathResult.ConsoleWrite();

            //The hard way (I mean I'm not telling you what to do..)
            math_reasoning mathResult2 = new math_reasoning();
            if (result.Response.OutputItems.LastOrDefault() is ModelMessageItem message)
            {
                Console.WriteLine($"[{message.Role}] {message?.Text}");

                using JsonDocument structuredJson = JsonDocument.Parse(message?.Text);
                mathResult2.final_answer = structuredJson.RootElement.GetProperty("final_answer").GetString()!;
                Console.WriteLine($"Final answer: {mathResult2.final_answer}");
                Console.WriteLine("Reasoning steps:");

                JsonElement.ArrayEnumerator steps = structuredJson.RootElement.GetProperty("steps").EnumerateArray();

                mathResult2.steps = new math_step[steps.Count()];
                int i = 0;
                foreach (JsonElement stepElement in steps)
                {
                    mathResult2.steps[i].explanation = stepElement.GetProperty("explanation").GetString() ?? "";
                    mathResult2.steps[i].output = stepElement.GetProperty("output").GetString() ?? "";
                    
                    Console.WriteLine($"  - Explanation: {mathResult2.steps[i].explanation}");
                    Console.WriteLine($"    Output: {mathResult2.steps[i].output}");

                    i++;
                }
            }
        }

        public struct math_reasoning
        {
            public math_step[] steps { get; set; }
            public string final_answer { get; set; }

            public void ConsoleWrite()
            {
                Console.WriteLine($"Final answer: {final_answer}");
                Console.WriteLine("Reasoning steps:");
                foreach (math_step step in steps)
                {
                    Console.WriteLine($"  - Explanation: {step.explanation}");
                    Console.WriteLine($"    Output: {step.output}");
                }
            }
        }

        public struct math_step
        {
            public string explanation { get; set; }
            public string output { get; set; }
        }
    }
}
