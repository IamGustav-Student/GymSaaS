using GymSaaS.SharedUI.Models;

namespace GymSaaS.SharedUI.Services
{
    public interface IApiService
    {
        bool IsAuthenticated { get; }
        bool IsOffline { get; }
        Task<bool> LoginAsync(string email, string password);
        Task<bool> TryRestoreSessionAsync();
        Task<List<T>?> GetAsync<T>(string endpoint);
        Task<T?> GetSingleAsync<T>(string endpoint);
        Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data);
        Task PutAsync<TRequest>(string endpoint, TRequest data);
        Task DeleteAsync(string endpoint);
    }
}
