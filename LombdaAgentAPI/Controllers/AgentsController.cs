using LombdaAgentAPI.Agents;
using LombdaAgentAPI.Models;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.AgentStateSystem;
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
        /// Get list of all available agent types
        /// </summary>
        /// <returns>List of agent type names</returns>
        [HttpGet("types")]
        public ActionResult<IEnumerable<string>> GetAgentTypes()
        {
            return Ok(_agentService.GetAgentTypes());
        }

        /// <summary>
        /// Create a new agent
        /// </summary>
        /// <param name="request">Agent creation request</param>
        /// <returns>Created agent info</returns>
        [HttpPost]
        public ActionResult<AgentResponse> CreateAgent(AgentCreationRequest request)
        {
            var agentId = _agentService.CreateAgent(request.Name, request.AgentType);
            if (agentId == "0")
            {
                return BadRequest($"Invalid agent type: {request.AgentType}. Use GET /v1/agents/types to see available types.");
            }
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

            if (agent is LombdaAgent apiAgent)
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
                    response = !string.IsNullOrEmpty(request.FileBase64Data) ? 
                        await agent.StartNewConversation(request.Text, request.FileBase64Data, false) :
                        await agent.StartNewConversation(request.Text, false);  
                }
                else
                {
                    response = !string.IsNullOrEmpty(request.FileBase64Data) ? await agent.AddBase64ImageToConversation(request.Text, request.FileBase64Data, false, request.ThreadId) : 
                                               await agent.AddToConversation(request.Text, request.ThreadId, false);
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
            var messageQueue = new System.Collections.Concurrent.ConcurrentQueue<ModelStreamingEvents>();
            var streamingComplete = false;
            var streamingError = false;
            var errorMessage = "";
            var eventsReceived = 0;

            // Enhanced streaming handler to handle different event types
            async Task StreamHandler(ModelStreamingEvents streamingEvent)
            {
                try
                {
                    eventsReceived++;
                    Console.WriteLine($"[STREAMING DEBUG] Event #{eventsReceived}: Type='{streamingEvent.EventType}', Status='{streamingEvent.Status}'");
                    
                    // Queue the event for async processing
                    //messageQueue.Enqueue(streamingEvent);
                    await ProcessStreamingEvent(streamingEvent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[STREAMING DEBUG] Error queuing streaming event: {ex.Message}");
                }
            }

            // Subscribe to agent streaming events
            agent.RootStreamingEvent += StreamHandler;
            Console.WriteLine($"[STREAMING DEBUG] Subscribed to RootStreamingEvent");

            try
            {
                // Send initial connection event
                await Response.WriteAsync("event: connected\n");
                await Response.WriteAsync("data: {\"status\": \"connected\"}\n\n");
                await Response.Body.FlushAsync();
                Console.WriteLine($"[STREAMING DEBUG] Sent connection event");

                // Start the agent processing in a separate task
                var agentTask = Task.Run(async () =>
                {
                    try
                    {
                        Console.WriteLine($"[STREAMING DEBUG] Starting agent conversation with streaming=true");
                        string response;
                        if (string.IsNullOrEmpty(request.ThreadId))
                        {
                            response = !string.IsNullOrEmpty(request.FileBase64Data) ? await agent.StartNewConversation(request.Text, request.FileBase64Data, true) :
                                await agent.StartNewConversation(request.Text, true);
                        }
                        else
                        {
                            response = !string.IsNullOrEmpty(request.FileBase64Data) ? await agent.AddBase64ImageToConversation(request.Text,request.FileBase64Data,true, request.ThreadId) : 
                            await agent.AddToConversation(request.Text, request.ThreadId, true);
                        }
                        
                        Console.WriteLine($"[STREAMING DEBUG] Agent completed. Response length: {response?.Length ?? 0}");
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
                    if (loopCount % 1000 == 0) // Log every 1000 iterations to avoid spam
                    {
                        Console.WriteLine($"[STREAMING DEBUG] Loop iteration {loopCount}, queue count: {messageQueue.Count}");
                    }

                    // Process all queued events
                    var eventsProcessed = 0;
                    while (messageQueue.TryDequeue(out var streamingEvent))
                    {
                        eventsProcessed++;
                        await ProcessStreamingEvent(streamingEvent);
                    }

                    // Small delay to prevent tight loop but maintain responsiveness
                    await Task.Delay(5);
                }

                Console.WriteLine($"[STREAMING DEBUG] Exited processing loop. Complete: {streamingComplete}, Error: {streamingError}");

                // Process any remaining events in the queue
                var remainingEvents = 0;
                while (messageQueue.TryDequeue(out var streamingEvent))
                {
                    remainingEvents++;
                    await ProcessStreamingEvent(streamingEvent);
                }
                
                if (remainingEvents > 0)
                {
                    Console.WriteLine($"[STREAMING DEBUG] Processed {remainingEvents} remaining events");
                }

                // Wait for agent task to complete and get the final response
                var finalResponse = await agentTask;

                Console.WriteLine($"[STREAMING DEBUG] Final summary - Events received: {eventsReceived}");

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

            // Helper method to process different streaming event types
            async Task ProcessStreamingEvent(ModelStreamingEvents streamingEvent)
            {
                switch (streamingEvent.EventType)
                {
                    case ModelStreamingEventType.Created:
                        await Response.WriteAsync($"event: created\n");
                        await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\"}}\n\n");
                        break;

                    case ModelStreamingEventType.OutputTextDelta:
                        if (streamingEvent is ModelStreamingOutputTextDeltaEvent deltaEvent)
                        {
                            await Response.WriteAsync($"event: delta\n");
                            await Response.WriteAsync($"data: {{\n");
                            await Response.WriteAsync($"data: \"sequenceId\": {deltaEvent.SequenceId},\n");
                            await Response.WriteAsync($"data: \"outputIndex\": {deltaEvent.OutputIndex},\n");
                            await Response.WriteAsync($"data: \"contentIndex\": {deltaEvent.ContentPartIndex},\n");
                            await Response.WriteAsync($"data: \"text\": \"{EscapeJsonString(deltaEvent.DeltaText ?? "")}\",\n");
                            await Response.WriteAsync($"data: \"itemId\": \"{deltaEvent.ItemId ?? ""}\"\n");
                            await Response.WriteAsync($"data: }}\n\n");
                            Console.WriteLine($"[STREAMING DEBUG] Sent delta: '{deltaEvent.DeltaText}'");
                        }
                        break;

                    case ModelStreamingEventType.Completed:
                        await Response.WriteAsync($"event: stream_complete\n");
                        await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\"}}\n\n");
                        break;

                    case ModelStreamingEventType.Error:
                        if (streamingEvent is ModelStreamingErrorEvent errorEvent)
                        {
                            await Response.WriteAsync($"event: stream_error\n");
                            await Response.WriteAsync($"data: {{\n");
                            await Response.WriteAsync($"data: \"sequenceId\": {errorEvent.SequenceId},\n");
                            await Response.WriteAsync($"data: \"error\": \"{EscapeJsonString(errorEvent.ErrorMessage ?? "")}\",\n");
                            await Response.WriteAsync($"data: \"code\": \"{EscapeJsonString(errorEvent.ErrorCode ?? "")}\"\n");
                            await Response.WriteAsync($"data: }}\n\n");
                        }
                        break;

                    case ModelStreamingEventType.ReasoningPartAdded:
                        if (streamingEvent is ModelStreamingReasoningPartAddedEvent reasoningEvent)
                        {
                            await Response.WriteAsync($"event: reasoning\n");
                            await Response.WriteAsync($"data: {{\n");
                            await Response.WriteAsync($"data: \"sequenceId\": {reasoningEvent.SequenceId},\n");
                            await Response.WriteAsync($"data: \"outputIndex\": {reasoningEvent.OutputIndex},\n");
                            await Response.WriteAsync($"data: \"text\": \"{EscapeJsonString(reasoningEvent.DeltaText ?? "")}\",\n");
                            await Response.WriteAsync($"data: \"itemId\": \"{reasoningEvent.ItemId}\"\n");
                            await Response.WriteAsync($"data: }}\n\n");
                        }
                        break;

                    default:
                        // For any other event types, send a generic event
                        await Response.WriteAsync($"event: {streamingEvent.EventType.ToString().ToLowerInvariant()}\n");
                        await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"status\": \"{streamingEvent.Status}\"}}\n\n");
                        break;
                }

                await Response.Body.FlushAsync();
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