using LombdaAgentAPI.Agents;
using LombdaAgentAPI.Models;
using LombdaAgentSDK.Agents.DataClasses;
using Microsoft.AspNetCore.Mvc;

namespace LombdaAgentAPI.Controllers
{
    /// <summary>
    /// Provides API endpoints for managing agents, including creating, retrieving, and sending messages to agents.
    /// </summary>
    /// <remarks>This controller handles HTTP requests related to agent operations. It supports retrieving a
    /// list of agents, creating new agents, fetching agent details by ID, and sending messages to agents with optional
    /// server-sent events streaming. The controller relies on an injected <see cref="ILombdaAgentService"/> to perform
    /// agent-related operations.</remarks>
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

            Console.WriteLine($"[STREAMING DEBUG] Starting stream for agent {id}");

            // Set proper SSE headers
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("X-Accel-Buffering", "no"); // Disable nginx buffering
            
            // Disable response buffering for real-time streaming
            var feature = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
            if (feature != null)
            {
                feature.DisableBuffering();
            }

            // Use a thread-safe queue for streaming messages
            var messageQueue = new System.Collections.Concurrent.ConcurrentQueue<string>();
            var streamingComplete = false;
            var streamingError = false;
            var errorMessage = "";
            var eventsReceived = 0;

            // DIAGNOSTIC: Track streaming events
            void StreamHandler(string message)
            {
                try
                {
                    eventsReceived++;
                    Console.WriteLine($"[STREAMING DEBUG] Event #{eventsReceived}: '{message}'");
                    // Queue the message for async processing
                    messageQueue.Enqueue(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[STREAMING DEBUG] Error queuing streaming data: {ex.Message}");
                }
            }

            // Subscribe to agent streaming events
            agent.RootStreamingEvent += StreamHandler;
            Console.WriteLine($"[STREAMING DEBUG] Subscribed to RootStreamingEvent");

            try
            {
                // Send initial heartbeat to establish the stream
                await Response.WriteAsync("data: \n\n");
                await Response.Body.FlushAsync();
                Console.WriteLine($"[STREAMING DEBUG] Sent initial heartbeat");

                // Start the agent processing in a separate task
                var agentTask = Task.Run(async () =>
                {
                    try
                    {
                        Console.WriteLine($"[STREAMING DEBUG] Starting agent conversation with streaming=true");
                        string response;
                        if (string.IsNullOrEmpty(request.ThreadId))
                        {
                            response = await agent.StartNewConversation(request.Text, true);
                        }
                        else
                        {
                            response = await agent.AddToConversation(request.Text, request.ThreadId, true);
                        }
                        
                        Console.WriteLine($"[STREAMING DEBUG] Agent completed. Response length: {response?.Length ?? 0}");
                        // Signal completion
                        streamingComplete = true;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[STREAMING DEBUG] Agent error: {ex.Message}");
                        streamingError = true;
                        errorMessage = ex.Message;
                        return null;
                    }
                });

                // Process the message queue in real-time
                Console.WriteLine($"[STREAMING DEBUG] Entering message processing loop");
                var loopCount = 0;
                while (!streamingComplete && !streamingError)
                {
                    loopCount++;
                    if (loopCount % 100 == 0) // Log every 100 iterations to avoid spam
                    {
                        Console.WriteLine($"[STREAMING DEBUG] Loop iteration {loopCount}, queue count: {messageQueue.Count}");
                    }

                    // Process all queued messages
                    var messagesProcessed = 0;
                    while (messageQueue.TryDequeue(out var message))
                    {
                        messagesProcessed++;
                        await Response.WriteAsync($"event: message\n");
                        await Response.WriteAsync($"data: {EscapeJsonString(message)}\n\n");
                        await Response.Body.FlushAsync();
                        Console.WriteLine($"[STREAMING DEBUG] Sent streaming message #{messagesProcessed}: '{message}'");
                    }

                    // Small delay to prevent tight loop but maintain responsiveness
                    await Task.Delay(10);
                }

                Console.WriteLine($"[STREAMING DEBUG] Exited processing loop. Complete: {streamingComplete}, Error: {streamingError}");

                // Process any remaining messages in the queue
                var remainingMessages = 0;
                while (messageQueue.TryDequeue(out var message))
                {
                    remainingMessages++;
                    await Response.WriteAsync($"event: message\n");
                    await Response.WriteAsync($"data: {EscapeJsonString(message)}\n\n");
                    await Response.Body.FlushAsync();
                }
                
                if (remainingMessages > 0)
                {
                    Console.WriteLine($"[STREAMING DEBUG] Processed {remainingMessages} remaining messages");
                }

                // Wait for agent task to complete and get the final response
                var finalResponse = await agentTask;

                Console.WriteLine($"[STREAMING DEBUG] Final summary - Events received: {eventsReceived}, Messages sent: {eventsReceived}");

                if (streamingError)
                {
                    await Response.WriteAsync($"event: error\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"error\": \"{EscapeJsonString(errorMessage)}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                    await Response.Body.FlushAsync();
                }
                else
                {
                    // Send the completion event
                    await Response.WriteAsync($"event: complete\n");
                    await Response.WriteAsync($"data: {{\n");
                    await Response.WriteAsync($"data: \"threadId\": \"{agent.MainThreadId}\",\n");
                    await Response.WriteAsync($"data: \"text\": \"{EscapeJsonString(finalResponse ?? "")}\"\n");
                    await Response.WriteAsync($"data: }}\n\n");
                    await Response.Body.FlushAsync();
                    Console.WriteLine($"[STREAMING DEBUG] Sent completion event");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STREAMING DEBUG] Exception in streaming: {ex.Message}");
                await Response.WriteAsync($"event: error\n");
                await Response.WriteAsync($"data: {{\n");
                await Response.WriteAsync($"data: \"error\": \"{EscapeJsonString(ex.Message)}\"\n");
                await Response.WriteAsync($"data: }}\n\n");
                await Response.Body.FlushAsync();
            }
            finally
            {
                // Always unsubscribe to prevent memory leaks
                agent.RootStreamingEvent -= StreamHandler;
                Console.WriteLine($"[STREAMING DEBUG] Unsubscribed from RootStreamingEvent");
            }
        }

        /// <summary>
        /// Test endpoint to manually trigger streaming events for debugging
        /// </summary>
        /// <param name="id">Agent ID</param>
        /// <returns>Test streaming response</returns>
        [HttpPost("{id}/test-stream")]
        public async Task TestStreamingEvents(string id)
        {
            var agent = _agentService.GetAgent(id);
            if (agent == null)
            {
                Response.StatusCode = 404;
                await Response.WriteAsync("Agent not found");
                return;
            }

            // Set proper SSE headers
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");

            try
            {
                // Send initial heartbeat
                await Response.WriteAsync("data: \n\n");
                await Response.Body.FlushAsync();

                // Manually trigger streaming events to test the infrastructure
                for (int i = 1; i <= 5; i++)
                {
                    await Response.WriteAsync($"event: message\n");
                    await Response.WriteAsync($"data: Test chunk {i}\n\n");
                    await Response.Body.FlushAsync();
                    await Task.Delay(500); // 500ms delay between chunks
                }

                // Send completion
                await Response.WriteAsync($"event: complete\n");
                await Response.WriteAsync($"data: {{\n");
                await Response.WriteAsync($"data: \"threadId\": \"test-thread\",\n");
                await Response.WriteAsync($"data: \"text\": \"Test completed\"\n");
                await Response.WriteAsync($"data: }}\n\n");
                await Response.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST STREAMING] Error: {ex.Message}");
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