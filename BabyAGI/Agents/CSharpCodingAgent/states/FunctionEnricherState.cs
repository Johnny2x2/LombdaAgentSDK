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
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.Agents.CSharpCodingAgent.states
{
    public struct FunctionEnrichment
    {
        public string description {  get; set; }
        public CommandLineArgs[] commandline_argument_examples { get; set; }
    }

    public struct CommandLineArgs
    { 
        public string input_args_array { get; set; }
    }

    internal class FunctionEnricherState : BaseState<CodeBuildInfoOutput, CodeBuildInfoOutput>
    {
        public CSHARP_CodingAgent StateAgent { get; set; }

        public FunctionEnricherState(CSHARP_CodingAgent stateAgent)
        {
            StateAgent = stateAgent;
        }
        
        public override async Task<CodeBuildInfoOutput> Invoke(CodeBuildInfoOutput input)
        {
            string prompt = 
                    $@"Original Task Breakdown:
                        {StateAgent.ProjectName}
                        
                        Sample Working Args from run: {input.ProgramResult.Result.Sample_EXE_Args}    

                        Files Generated:
                        {StateAgent.GetFilesTool()}
                        
                        Program.cs:
                        {StateAgent.ReadFileTool("Program.cs")}
                    ";

            LLMTornadoModelProvider client = new(
                ChatModel.OpenAi.Gpt41.V41Mini,
                [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            Agent agent = new Agent(client,
                "Enricher Assistant",
                "You are an expert C# programmer. Your task is to enrich the generated function information to allow another AI to be able to Understand what it does, and how to use it.\n"+
                "Generate a short description of the function so an agent can decide from a list of functions when to use it.\n"+
                "To help the agent figure out how to use the function with EXE Args Generate 5-10 working Example input args " +
                "to use for the function when running EXE process process.StartInfo.Arguments = arguments; ",
                _output_schema: typeof(FunctionEnrichment));

            RunResult result = await Runner.RunAsync(agent, prompt);

            FunctionEnrichment enrichment = result.ParseJson<FunctionEnrichment>();

            FunctionGeneratorUtility.WriteProjectDescription(StateAgent.FunctionsPath, StateAgent.ProjectName, enrichment.description);

            ConcurrentDictionary<string, ExecutableOutputResult> argsConcurrentResults = new();

            List<Task> tasks = new List<Task>();

            foreach (var arg in enrichment.commandline_argument_examples)
            {
                tasks.Add(Task.Run(async () => argsConcurrentResults.TryAdd(arg.input_args_array, await FunctionGeneratorUtility.FindAndRunExecutableAndCaptureOutput(StateAgent.FunctionsPath, StateAgent.ProjectName, "net8.0", arg.input_args_array))));
            }

            tasks.Add(Task.Run(async () => argsConcurrentResults.TryAdd(
                input.ProgramResult.Result.Sample_EXE_Args, 
                await FunctionGeneratorUtility.FindAndRunExecutableAndCaptureOutput(StateAgent.FunctionsPath, StateAgent.ProjectName, "net8.0", input.ProgramResult.Result.Sample_EXE_Args))));

            await Task.WhenAll(tasks);

            FunctionGeneratorUtility.WriteProjectArgs(
                StateAgent.FunctionsPath, 
                StateAgent.ProjectName, 
                string.Join("\n\n", argsConcurrentResults.Where(r => r.Value.ExecutionCompleted).Select(arg => $"<ArgsSample><InputArgs>{arg.Key}</InputArgs>\n<Result>{arg.Value.Output.Trim()}</Result></ArgsSample>")));

            await AddFunctionDescriptionIntoDB(enrichment);

            return input;
        }

        public async Task AddFunctionDescriptionIntoDB(FunctionEnrichment enrichment)
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

            Dictionary<string, object> functionMetaData = new Dictionary<string, object>();

            functionMetaData.Add("FunctionName", StateAgent.ProjectName);

            TornadoApi tornadoApi = new TornadoApi([new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);
            EmbeddingResult? embresult = await tornadoApi.Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen2.Ada, enrichment.description);

            float[]? data = embresult?.Data.FirstOrDefault()?.Embedding;

            await functionClient.Add([Guid.NewGuid().ToString()], embeddings: [new(data)], metadatas: [functionMetaData], documents: [enrichment.description]);
        }
    }
}
