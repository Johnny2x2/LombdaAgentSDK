using LombdaAgentMAUI.Core.Services;

namespace LombdaAgentMAUI.Tests.Mocks
{
    public class MockSecureStorageService : ISecureStorageService
    {
        private readonly Dictionary<string, string> _storage = new();

        public Task<string?> GetAsync(string key)
        {
            _storage.TryGetValue(key, out var value);
            return Task.FromResult(value);
        }

        public Task SetAsync(string key, string value)
        {
            _storage[key] = value;
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _storage.Remove(key);
        }

        public void Clear()
        {
            _storage.Clear();
        }

        public bool ContainsKey(string key)
        {
            return _storage.ContainsKey(key);
        }

        public int Count => _storage.Count;
    }
}