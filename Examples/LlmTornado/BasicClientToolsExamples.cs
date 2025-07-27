using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Images.Models;
using LlmTornado.Responses;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.LlmTornado
{
    public class BasicClientToolsExamples
    {
        [Test]
        public async Task RunBasicTornadoVectorStoreUse()
        {
            LLMTornadoModelProvider client = new(
                ChatModel.OpenAi.Gpt41.V41Mini,
                [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),],
                useResponseAPI: true,
                searchOptions: new VectorSearchOptions(["vs_6884c6c13814819187cec68d16415425"]));

            Agent agent = new Agent(client,
                "Assistant",
                "You are a useful assistant.");

            RunResult result = await Runner.RunAsync(agent, "Can you tell me about table 1?");

            Console.WriteLine(result.Text);
        }


            [Test]
            public async Task RunBasicTornadoCodeInterpreterUse()
            {
                LLMTornadoModelProvider client = new(
                    ChatModel.OpenAi.Gpt41.V41Mini,
                    [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),],
                    useResponseAPI: true,
                    codeOptions:new ModelCodeInterpreterOptions() { FileIds = ["file-K2c2KRMcbDdF4uecxEmnuF"] });

                Agent agent = new Agent(client,
                    "Assistant",
                    "You are a useful assistant.");

                RunResult result = await Runner.RunAsync(agent, "Can you count the times LLM is used in the document?");

                Console.WriteLine(result.Text);
            }

        [Test]
        public static async Task ResponseDeepResearchMcp()
        {
            EndpointBase.SetRequestsTimeout(20000);

            TornadoApi api = new TornadoApi(
                [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!)]);

            ResponseResult result = await api.Responses.CreateResponse(new ResponseRequest
            {
                Model = ChatModel.OpenAi.Gpt41.V41,
                Background = false,
                InputItems = [
                    new ResponseInputMessage(ChatMessageRoles.User, "Research detailed information about latest development in the Ukraine war and predict how long will Pokrovsk hold.  Please send e-mail analysis to EMAIL using the MCP.")
                ],
                Tools = [
                new ResponseMcpTool
                {
                    ServerLabel = "scriptwriter",
                    ServerUrl = "http://localhost:8000",
                    RequireApproval = ResponseMcpRequireApprovalOption.Never
                }
                ]
            });

            int z = 0;
        }
    }
}
