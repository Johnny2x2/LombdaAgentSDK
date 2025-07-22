using LombdaAgentAPI.Agents;
using LombdaAgentAPI.Models;
using LombdaAgentSDK.Agents.DataClasses;
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

            // Setup action to handle agent streaming events using the new ModelStreamingEvents system
            async Task StreamHandler(ModelStreamingEvents streamingEvent)
            {
                try
                {
                    // Format different event types appropriately for SSE
                    await FormatAndSendStreamingEvent(streamingEvent, token);
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
                // Send initial connection event
                await Response.WriteAsync("event: connected\n");
                await Response.WriteAsync("data: {\"status\": \"connected\", \"agentId\": \"" + id + "\"}\n\n");
                await Response.Body.FlushAsync(token);

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

        /// <summary>
        /// Helper method to format and send different types of streaming events
        /// </summary>
        private async Task FormatAndSendStreamingEvent(ModelStreamingEvents streamingEvent, CancellationToken token)
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
                        await Response.WriteAsync($"event: text_delta\n");
                        await Response.WriteAsync($"data: {{\n");
                        await Response.WriteAsync($"data: \"sequenceId\": {deltaEvent.SequenceId},\n");
                        await Response.WriteAsync($"data: \"outputIndex\": {deltaEvent.OutputIndex},\n");
                        await Response.WriteAsync($"data: \"contentIndex\": {deltaEvent.ContentPartIndex},\n");
                        await Response.WriteAsync($"data: \"text\": \"{EscapeJsonString(deltaEvent.DeltaText ?? "")}\",\n");
                        await Response.WriteAsync($"data: \"itemId\": \"{deltaEvent.ItemId ?? ""}\"\n");
                        await Response.WriteAsync($"data: }}\n\n");
                    }
                    break;

                case ModelStreamingEventType.Completed:
                    await Response.WriteAsync($"event: completed\n");
                    await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"responseId\": \"{streamingEvent.ResponseId}\"}}\n\n");
                    break;

                case ModelStreamingEventType.Error:
                    if (streamingEvent is ModelStreamingErrorEvent errorEvent)
                    {
                        await Response.WriteAsync($"event: error\n");
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
                    await Response.WriteAsync($"data: {{\"sequenceId\": {streamingEvent.SequenceId}, \"status\": \"{streamingEvent.Status}\", \"type\": \"{streamingEvent.EventType}\"}}\n\n");
                    break;
            }

            await Response.Body.FlushAsync(token);
        }

        /// <summary>
        /// Helper method to escape JSON strings for safe transmission
        /// </summary>
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