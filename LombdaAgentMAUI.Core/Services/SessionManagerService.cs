using LombdaAgentMAUI.Core.Models;
using System.Text.Json;

namespace LombdaAgentMAUI.Core.Services
{
    /// <summary>
    /// Interface for session management service
    /// </summary>
    public interface ISessionManagerService
    {
        /// <summary>
        /// Get session for a specific agent
        /// </summary>
        Task<AgentSession?> GetSessionAsync(string agentId);

        /// <summary>
        /// Save or update session for an agent
        /// </summary>
        Task SaveSessionAsync(AgentSession session);

        /// <summary>
        /// Get all sessions
        /// </summary>
        Task<Dictionary<string, AgentSession>> GetAllSessionsAsync();

        /// <summary>
        /// Clear session for a specific agent
        /// </summary>
        Task ClearSessionAsync(string agentId);

        /// <summary>
        /// Clear all sessions
        /// </summary>
        Task ClearAllSessionsAsync();

        /// <summary>
        /// Get the last selected agent ID
        /// </summary>
        Task<string?> GetLastSelectedAgentIdAsync();

        /// <summary>
        /// Save the last selected agent ID
        /// </summary>
        Task SaveLastSelectedAgentIdAsync(string agentId);
    }

    /// <summary>
    /// Session manager service implementation using secure storage
    /// </summary>
    public class SessionManagerService : ISessionManagerService
    {
        private readonly ISecureStorageService _secureStorage;
        private const string SessionsKey = "agent_sessions";
        private const string LastSelectedAgentKey = "last_selected_agent";
        private readonly JsonSerializerOptions _jsonOptions;
        private SessionData? _cachedSessionData;

        public SessionManagerService(ISecureStorageService secureStorage)
        {
            _secureStorage = secureStorage;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        /// <summary>
        /// Load session data from storage or create new
        /// </summary>
        private async Task<SessionData> LoadSessionDataAsync()
        {
            if (_cachedSessionData != null)
            {
                return _cachedSessionData;
            }

            try
            {
                var json = await _secureStorage.GetAsync(SessionsKey);
                if (!string.IsNullOrEmpty(json))
                {
                    _cachedSessionData = JsonSerializer.Deserialize<SessionData>(json, _jsonOptions);
                }
            }
            catch (Exception)
            {
                // If deserialization fails, start fresh
            }

            _cachedSessionData ??= new SessionData();
            return _cachedSessionData;
        }

        /// <summary>
        /// Save session data to storage
        /// </summary>
        private async Task SaveSessionDataAsync(SessionData sessionData)
        {
            try
            {
                var json = JsonSerializer.Serialize(sessionData, _jsonOptions);
                await _secureStorage.SetAsync(SessionsKey, json);
                _cachedSessionData = sessionData;
            }
            catch (Exception)
            {
                // Handle save errors gracefully
            }
        }

        public async Task<AgentSession?> GetSessionAsync(string agentId)
        {
            var sessionData = await LoadSessionDataAsync();
            return sessionData.Sessions.TryGetValue(agentId, out var session) ? session : null;
        }

        public async Task SaveSessionAsync(AgentSession session)
        {
            var sessionData = await LoadSessionDataAsync();
            session.LastActivity = DateTime.Now;
            sessionData.Sessions[session.AgentId] = session;
            await SaveSessionDataAsync(sessionData);
        }

        public async Task<Dictionary<string, AgentSession>> GetAllSessionsAsync()
        {
            var sessionData = await LoadSessionDataAsync();
            return new Dictionary<string, AgentSession>(sessionData.Sessions);
        }

        public async Task ClearSessionAsync(string agentId)
        {
            var sessionData = await LoadSessionDataAsync();
            sessionData.Sessions.Remove(agentId);
            await SaveSessionDataAsync(sessionData);
        }

        public async Task ClearAllSessionsAsync()
        {
            var sessionData = new SessionData();
            await SaveSessionDataAsync(sessionData);
        }

        public async Task<string?> GetLastSelectedAgentIdAsync()
        {
            var sessionData = await LoadSessionDataAsync();
            return sessionData.LastSelectedAgentId;
        }

        public async Task SaveLastSelectedAgentIdAsync(string agentId)
        {
            var sessionData = await LoadSessionDataAsync();
            sessionData.LastSelectedAgentId = agentId;
            await SaveSessionDataAsync(sessionData);
        }
    }
}