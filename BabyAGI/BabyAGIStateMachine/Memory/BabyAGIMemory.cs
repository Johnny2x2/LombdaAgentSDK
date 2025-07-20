using BabyAGI.Agents.CSharpCodingAgent.states;
using BabyAGI.BabyAGIStateMachine.DataModels;
using BabyAGI.Utility;
using ChromaDB.Client;
using LlmTornado;
using LlmTornado.Code;
using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.BabyAGIStateMachine.Memory
{
    public static class BabyAGIMemory
    {
        public static async Task ClearShortTermMemory()
        {
            var handler = new ApiV1ToV2DelegatingHandler
            {
                InnerHandler = new HttpClientHandler()
            };
            var configOptions = new ChromaConfigurationOptions(uri: BabyAGIConfig.ChromaDbURI);
            using var httpClient = new HttpClient(handler);
            var chromaClient = new ChromaClient(configOptions, httpClient);
            await chromaClient.DeleteCollection("ShortTerm");
        }
        public static async Task ClearLongTermMemory()
        {
            var handler = new ApiV1ToV2DelegatingHandler
            {
                InnerHandler = new HttpClientHandler()
            };
            var configOptions = new ChromaConfigurationOptions(uri: BabyAGIConfig.ChromaDbURI);
            using var httpClient = new HttpClient(handler);
            var chromaClient = new ChromaClient(configOptions, httpClient);
            await chromaClient.DeleteCollection("LongTerm");
        }
        private static async Task AddTaskResultToMemory(string task, string result, string collection, MetadataKeyValuePair[] metadata = null)
        {
            var handler = new ApiV1ToV2DelegatingHandler
            {
                InnerHandler = new HttpClientHandler()
            };

            var configOptions = new ChromaConfigurationOptions(uri: BabyAGIConfig.ChromaDbURI);
            using var httpClient = new HttpClient(handler);
            var chromaClient = new ChromaClient(configOptions, httpClient);

            var functionCollection = await chromaClient.GetOrCreateCollection(collection);
            var functionClient = new ChromaCollectionClient(functionCollection, configOptions, httpClient);

            Dictionary<string, object> MetaData = new Dictionary<string, object>();

            MetaData.Add("Task", task);

            if (metadata != null)
            {
                foreach (var item in metadata)
                {
                    MetaData.Add(item.Key, item.Value);
                }
            }

            TornadoApi tornadoApi = new TornadoApi([new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);
            EmbeddingResult? embresult = await tornadoApi.Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen3.Small, task);

            float[]? data = embresult?.Data.FirstOrDefault()?.Embedding;

            await functionClient.Add([Guid.NewGuid().ToString()], embeddings: [new(data)], metadatas: [MetaData], documents: [result]);
        }

        private static async Task<string[]?> QueryMemory(string query, string collection)
        {
            var handler = new ApiV1ToV2DelegatingHandler
            {
                InnerHandler = new HttpClientHandler()
            };

            var configOptions = new ChromaConfigurationOptions(uri: BabyAGIConfig.ChromaDbURI);
            using var httpClient = new HttpClient(handler);
            var chromaClient = new ChromaClient(configOptions, httpClient);

            var functionCollection = await chromaClient.GetOrCreateCollection(collection);
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
                    closeFunctions.Add($"Task: {entry.Metadata?["Task"]} \n Result: {entry.Document}");
                }
            }

            return closeFunctions.ToArray();
        }

        public static async Task<string[]?> QueryShortTermMemory(string query)
        {
            return await QueryMemory(query, "ShortTerm");
        }

        public static async Task<string[]?> QueryLongTermMemory(string query)
        {
            return await QueryMemory(query, "LongTerm");
        }
        public static async Task AddTaskResultShortTermMemory(string task, string result, MetadataKeyValuePair[] metadata = null)
        {
            await AddTaskResultToMemory(task, result, "ShortTerm", metadata);
        }

        public static async Task AddTaskResultToLongTermMemory(string task, string result, MetadataKeyValuePair[] metadata = null)
        {
            await AddTaskResultToMemory(task, result, "LongTerm", metadata);
        }
    }
}
