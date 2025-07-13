using BabyAGI.Utility;
using ChromaDB.Client;
using Examples.Demos.CodingAgent;
using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.StateMachine;

namespace Examples.Demos.FunctionGenerator.States
{
    public struct FunctionFoundResult
    {
        public string FunctionName { get; set; }
        public bool FunctionFound { get; set; }
        public FunctionFoundResult(string functionName, bool functionFound) { FunctionName = functionName;  FunctionFound = functionFound; }
    }

    public struct FunctionFoundResultOutput
    {
        public string UserInput { get; set; }
        public FunctionFoundResult FoundResult { get; set; }
        public FunctionFoundResultOutput(string userInput, FunctionFoundResult functionFound) 
        { 
            UserInput = userInput; 
            FoundResult = functionFound;
        }
    }

    public class CheckExistingFunctionState : BaseState<string, FunctionFoundResultOutput>
    {
        public FunctionGeneratorAgent StateController { get; set; }
        public CheckExistingFunctionState(FunctionGeneratorAgent stateController) { StateController = stateController; }
        public async override Task<FunctionFoundResultOutput> Invoke(string args)
        {
            string[]? functions = await QueryFunctionDB(args);

            functions ??= [];

            string instructions = string.Format("""
            
            
            The user has provided the following request:

            "{0}"

            Task from the Review Agent:
            {1}

            Below is a list of available functions with their descriptions:

            {2}

            """, StateController.OriginalTask, args, string.Join("\n\n", functions));

            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            Agent agent = new Agent(client,
                "Code Assistant",
                "You are an expert at reviewing functions and determining which to use to solve the Task at hand. If no suitable Function is found let the system know and a new one will be generated.",
                _output_schema: typeof(FunctionFoundResult));

            RunResult result = await Runner.RunAsync(agent, instructions);

            return new FunctionFoundResultOutput(args, result.ParseJson<FunctionFoundResult>());
        }

        public async Task<string[]?> QueryFunctionDB(string query)
        {
            var handler = new ApiV1ToV2DelegatingHandler
            {
                InnerHandler = new HttpClientHandler()
            };

            var configOptions = new ChromaConfigurationOptions(uri: BabyAGIConfig.ChromaDbURI);
            using var httpClient = new HttpClient(handler);
            var chromaClient = new ChromaClient(configOptions, httpClient);

            var functionCollection = await chromaClient.GetOrCreateCollection("functionCollection");
            var functionClient = new ChromaCollectionClient(functionCollection, configOptions, httpClient);

            TornadoApi tornadoApi = new TornadoApi([new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);
            EmbeddingResult? embresult = await tornadoApi.Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen3.Small, query);

            float[]? data = embresult?.Data.FirstOrDefault()?.Embedding;

            var queryData = await functionClient.Query([new(data)], include: ChromaQueryInclude.Metadatas | ChromaQueryInclude.Distances | ChromaQueryInclude.Documents);

            List<string> closeFunctions = new List<string>();

            foreach (var item in queryData)
            {
                foreach (var entry in item)
                {
                    closeFunctions.Add($"Function Name: {entry.Metadata?["FunctionName"]} \n Description: {entry.Document}");
                }
            }

            return closeFunctions.ToArray();
        }
    }
}
