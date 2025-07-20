using LombdaAgentAPI.Agents;
using Microsoft.AspNetCore.SignalR;

namespace LombdaAgentAPI.Hubs
{
    public class AgentHub : Hub
    {
        private readonly ILombdaAgentService _agentService;

        public AgentHub(ILombdaAgentService agentService)
        {
            _agentService = agentService;
        }

        /// <summary>
        /// Subscribe to agent streaming events
        /// </summary>
        /// <param name="agentId">Agent ID to subscribe to</param>
        /// <returns>True if subscription successful</returns>
        public async Task<bool> SubscribeToAgent(string agentId)
        {
            var result = _agentService.AddStreamingSubscriber(agentId, Context.ConnectionId);
            if (result)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"agent-{agentId}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// When client disconnects, clean up subscriptions
        /// </summary>
        /// <param name="exception">Exception if any</param>
        /// <returns>Task</returns>
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _agentService.RemoveStreamingSubscriber(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}