using LombdaAgentAPI.Agents;
using LombdaAgentAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LombdaAgentAPI.Controllers
{
    /// <summary>
    /// Provides endpoints for streaming events from agents using Server-Sent Events (SSE).
    /// </summary>
    /// <remarks>This controller is responsible for managing the streaming of events from agents. It uses
    /// Server-Sent Events to push updates to clients in real-time. The controller listens for events from agents and
    /// forwards them to connected clients.</remarks>
    [ApiController]
    [Route("v1/[controller]")]
    public class StreamController : ControllerBase
    {
        private readonly ILombdaAgentService _agentService;
        
        public StreamController(ILombdaAgentService agentService)
        {
            _agentService = agentService;
        }

        /// <summary>
        /// Stream events from an agent using Server-Sent Events
        /// </summary>
        /// <param name="id">Agent ID</param>
        /// <returns>SSE stream</returns>
        [HttpGet("agents/{id}")]
        public async Task StreamAgentEvents(string id)
        {
            var agent = _agentService.GetAgent(id);
            if (agent == null)
            {
                Response.StatusCode = 404;
                await Response.WriteAsync("Agent not found");
                return;
            }

            // Set SSE headers
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            // Create a cancellation token source for this connection
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // Setup action to handle agent streaming events
            void StreamHandler(string message)
            {
                try
                {
                    // Format as SSE message
                    var task = Response.WriteAsync($"event: message\n");
                    task.Wait(token);
                    task = Response.WriteAsync($"data: {message}\n\n");
                    task.Wait(token);
                    task = Response.Body.FlushAsync(token);
                    task.Wait(token);
                }
                catch (OperationCanceledException)
                {
                    // Connection closed, we'll handle this below
                }
                catch (Exception)
                {
                    // Other errors - may need to terminate the stream
                    cancellationTokenSource.Cancel();
                }
            }

            // Subscribe to agent streaming events
            agent.RootStreamingEvent += StreamHandler;

            try
            {
                // Keep the connection open until cancellation
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1000, token);
                    
                    // Send a heartbeat to keep the connection alive
                    await Response.WriteAsync(":\n\n");
                    await Response.Body.FlushAsync(token);
                }
            }
            catch (OperationCanceledException)
            {
                // This is expected when the client disconnects
            }
            finally
            {
                // Clean up
                agent.RootStreamingEvent -= StreamHandler;
                cancellationTokenSource.Dispose();
            }
        }
    }
}