using System.Net.Http.Json;
using PSA.WebApp.Models.Fincas;

namespace PSA.WebApp.Services
{
    public class FincaApiService
    {
        private readonly HttpClient _httpClient;

        public FincaApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<FincaViewModel>> RetrieveAllAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<FincaViewModel>>("api/Finca/RetrieveAll");
                return response ?? new List<FincaViewModel>();
            }
            catch
            {
                return new List<FincaViewModel>();
            }
        }

        public async Task<FincaViewModel?> RetrieveByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<FincaViewModel>($"api/Finca/RetrieveById/{id}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Ok, string Error)> CreateAsync(FincaViewModel finca)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/Finca/Create", finca);

                if (response.IsSuccessStatusCode)
                    return (true, "");

                var errorBody = await response.Content.ReadAsStringAsync();
                return (false, errorBody);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}