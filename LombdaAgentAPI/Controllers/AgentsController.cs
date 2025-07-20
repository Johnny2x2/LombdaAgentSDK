using LombdaAgentAPI.Agents;
using LombdaAgentAPI.Models;
using LombdaAgentSDK.Agents.DataClasses;
using Microsoft.AspNetCore.Mvc;

namespace LombdaAgentAPI.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class AgentsController : ControllerBase
    {
        private readonly ILombdaAgentService _agentService;

        public AgentsController(ILombdaAgentService agentService)
        {
            _agentService = agentService;
        }

        /// <summary>
        /// Get list of all agents
        /// </summary>
        /// <returns>List of agent IDs</returns>
        [HttpGet]
        public ActionResult<IEnumerable<string>> GetAgents()
        {
            return Ok(_agentService.GetAgentIds());
        }

        /// <summary>
        /// Create a new agent
        /// </summary>
        /// <param name="request">Agent creation request</param>
        /// <returns>Created agent info</returns>
        [HttpPost]
        public ActionResult<AgentResponse> CreateAgent(AgentCreationRequest request)
        {
            var agentId = _agentService.CreateAgent(request.Name);
            return Ok(new AgentResponse { Id = agentId, Name = request.Name });
        }

        /// <summary>
        /// Get agent by ID
        /// </summary>
        /// <param name="id">Agent ID</param>
        /// <returns>Agent details or NotFound</returns>
        [HttpGet("{id}")]
        public ActionResult<AgentResponse> GetAgent(string id)
        {
            var agent = _agentService.GetAgent(id);
            if (agent == null)
                return NotFound();

            if (agent is APILombdaAgent apiAgent)
            {
                return Ok(new AgentResponse
                {
                    Id = apiAgent.AgentId,
                    Name = apiAgent.AgentName
                });
            }

            return Ok(new AgentResponse { Id = id, Name = "Unknown" });
        }

        /// <summary>
        /// Send a message to an agent and get the response
        /// </summary>
        /// <param name="id">Agent ID</param>
        /// <param name="request">Message request</param>
        /// <returns>Agent response</returns>
        [HttpPost("{id}/messages")]
        public async Task<ActionResult<MessageResponse>> SendMessage(string id, MessageRequest request)
        {
            var agent = _agentService.GetAgent(id);
            if (agent == null)
                return NotFound();

            try
            {
                string response;
                if (string.IsNullOrEmpty(request.ThreadId))
                {
                    response = await agent.StartNewConversation(request.Text, false);
                }
                else
                {
                    response = await agent.AddToConversation(request.Text, request.ThreadId, false);
                }

                return Ok(new MessageResponse
                {
                    AgentId = id,
                    ThreadId = agent.MainThreadId,
                    Text = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing message: {ex.Message}");
            }
        }

        /// <summary>
        /// Send a message to an agent with server-sent events streaming
        /// </summary>
        /// <param name="id">Agent ID</param>
        /// <param name="request">Message request</param>
        /// <returns>Streaming response</returns>
        [HttpPost("{id}/messages/stream")]
        public async Task SendMessageStream(string id, [FromBody] MessageRequest request)
        {
            var agent = _agentService.GetAgent(id);
            if (agent == null)
            {
                Response.StatusCode = 404;
                await Response.WriteAsync("Agent not found");
                return;
            }

            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            // Setup local streaming to capture the output
            var buffer = new List<string>();
            agent.RootStreamingEvent += (message) => 
            {
                buffer.Add(message);
                return;
            };

            string response;
            try
            {
                // Process the message
                if (string.IsNullOrEmpty(request.ThreadId))
                {
                    response = await agent.StartNewConversation(request.Text, true);
                }
                else
                {
                    response = await agent.AddToConversation(request.Text, request.ThreadId, true);
                }

                // Write the completion event
                await Response.WriteAsync($"event: complete\n");
                await Response.WriteAsync($"data: {{\n");
                await Response.WriteAsync($"data: \"threadId\": \"{agent.MainThreadId}\",\n");
                await Response.WriteAsync($"data: \"text\": \"{EscapeJsonString(response)}\"\n");
                await Response.WriteAsync($"data: }}\n\n");
                await Response.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                await Response.WriteAsync($"event: error\n");
                await Response.WriteAsync($"data: {{\n");
                await Response.WriteAsync($"data: \"error\": \"{EscapeJsonString(ex.Message)}\"\n");
                await Response.WriteAsync($"data: }}\n\n");
                await Response.Body.FlushAsync();
            }
        }

        private static string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}