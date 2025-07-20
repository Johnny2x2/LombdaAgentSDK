using LombdaAgentAPI.Agents;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LombdaAgentAPI.Tests.Integration
{
    /// <summary>
    /// Custom WebApplicationFactory for testing the LombdaAgentAPI
    /// </summary>
    public class CustomApiFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Replace real services with mocks for testing
            builder.ConfigureServices(services => 
            {
                // Replace the real agent service with our mock implementation
                services.AddSingleton<ILombdaAgentService, MockLombdaAgentService>();
            });

            return base.CreateHost(builder);
        }
    }

    /// <summary>
    /// Base class for all API integration tests
    /// </summary>
    public abstract class TestBase
    {
        protected WebApplicationFactory<Program> _factory = null!;
        protected HttpClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = new CustomApiFactory();
            _client = _factory.CreateClient();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        /// <summary>
        /// Helper to serialize request objects to JSON
        /// </summary>
        protected StringContent SerializeToJson(object obj)
        {
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        /// <summary>
        /// Helper to deserialize JSON responses
        /// </summary>
        protected async Task<T> DeserializeResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
        }
    }
}