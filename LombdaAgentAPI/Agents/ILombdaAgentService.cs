using LombdaAgentSDK.AgentStateSystem;
using System.Collections.Concurrent;

namespace LombdaAgentAPI.Agents
{
    public interface ILombdaAgentService
    {
        /// <summary>
        /// Gets all available agent IDs
        /// </summary>
        /// <returns>List of agent IDs</returns>
        List<string> GetAgentIds();

        /// <summary>
        /// Gets agent by ID
        /// </summary>
        /// <param name="agentId">The agent ID</param>
        /// <returns>LombdaAgent instance or null if not found</returns>
        LombdaAgent? GetAgent(string agentId);

        /// <summary>
        /// Creates a new agent
        /// </summary>
        /// <param name="agentName">Name for the agent</param>
        /// <returns>The agent ID</returns>
        string CreateAgent(string agentName);

        /// <summary>
        /// Adds a subscriber for agent streaming events
        /// </summary>
        /// <param name="agentId">Agent ID</param>
        /// <param name="connectionId">SignalR connection ID</param>
        /// <returns>True if successful</returns>
        bool AddStreamingSubscriber(string agentId, string connectionId);

        /// <summary>
        /// Removes a subscriber from agent streaming events
        /// </summary>
        /// <param name="connectionId">SignalR connection ID</param>
        void RemoveStreamingSubscriber(string connectionId);
    }
}