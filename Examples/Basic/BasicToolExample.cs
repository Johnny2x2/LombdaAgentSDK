using LombdaAgentSDK.Agents;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;

namespace Examples.Basic
{
    internal class BasicToolExample
    {
        [Test]
        public  async Task RunBasicToolExample()
        {
            Agent agent = new Agent(
                new OpenAIModelClient("gpt-4o-mini"), 
                "Assistant", 
                "Have fun",  
                _tools : [GetCurrentLocation, GetCurrentWeather]);

            RunResult result = await Runner.RunAsync(agent, "What is the weather in boston?");

            Console.WriteLine($"[ASSISTANT]: {result.Text}");
        }

        [Tool(Description = "Get the user's current location")]
        public string GetCurrentLocation()
        {
            // Call the location API here.
            return "San Francisco";
        }

        public enum Unit { celsius, fahrenheit }
        [Tool(
            Description = "Get the current weather in a given location",
            In_parameters_description = [
                "The city and state, e.g. Boston, MA",
                "The temperature unit to use. Infer this from the specified location."
                ])]
        public string GetCurrentWeather(string location, Unit unit = Unit.celsius)
        {
            // Call the weather API here.
            return $"31 C";
        }  
    }
}
