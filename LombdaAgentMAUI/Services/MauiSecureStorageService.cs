using LombdaAgentMAUI.Core.Services;

namespace LombdaAgentMAUI.Services
{
    /// <summary>
    /// MAUI-specific implementation of secure storage using Microsoft.Maui.Storage.SecureStorage
    /// </summary>
    public class MauiSecureStorageService : ISecureStorageService
    {
        public async Task<string?> GetAsync(string key)
        {
            return await Microsoft.Maui.Storage.SecureStorage.GetAsync(key);
        }

        public async Task SetAsync(string key, string value)
        {
            await Microsoft.Maui.Storage.SecureStorage.SetAsync(key, value);
        }

        public void Remove(string key)
        {
            Microsoft.Maui.Storage.SecureStorage.Remove(key);
        }
    }
}