using System.Net.Http;
using System.Net.Http.Json;

namespace CC.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5062/")
            };
        }

        public void SetSessionHeaders(int userId, int companyId)
        {
            _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
            _httpClient.DefaultRequestHeaders.Remove("X-Company-Id");

            _httpClient.DefaultRequestHeaders.Add(
                "X-User-Id",
                userId.ToString()
            );

            _httpClient.DefaultRequestHeaders.Add(
                "X-Company-Id",
                companyId.ToString()
            );
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync(endpoint);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(
    string endpoint, TRequest data)
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();

                throw new Exception(
                    $"API Error ({(int)response.StatusCode}): {errorMessage}"
                );
            }

            return await response.Content.ReadFromJsonAsync<TResponse>();
        }

        public async Task PutAsync<TRequest>(
            string endpoint,
            TRequest data)
        {
            var response = await _httpClient.PutAsJsonAsync(
                endpoint,
                data
            );

            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string endpoint)
        {
            var response = await _httpClient.DeleteAsync(endpoint);

            response.EnsureSuccessStatusCode();
        }
    }
}
