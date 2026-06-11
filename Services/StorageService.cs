using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetCrawler.Services
{
    public class StorageService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _scopeFactory;

        public StorageService(HttpClient httpClient, IConfiguration config, IServiceScopeFactory scopeFactory)
        {
            _httpClient = httpClient;
            _config = config;
            _scopeFactory = scopeFactory;
        }

        public async Task<bool> UploadImageStreamAsync(string objectKey, Stream contentStream, string contentType)
        {
            string? apiUrl = null;
            string? bucket = null;
            string? token = null;

            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DotnetCrawler.Data.AppDbContext>();
                var urlSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "STORAGE_API_URL");
                var bucketSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "STORAGE_BUCKET");
                var tokenSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "STORAGE_TOKEN");

                apiUrl = urlSetting?.Value ?? _config["STORAGE_API_URL"];
                bucket = bucketSetting?.Value ?? _config["STORAGE_BUCKET"];
                token = tokenSetting?.Value ?? _config["STORAGE_TOKEN"];
            }

            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(bucket))
            {
                // If storage not configured, return false
                return false;
            }

            var requestUrl = $"{apiUrl.TrimEnd('/')}/{bucket}/{objectKey}";

            var request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
            
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Add("X-Bucket-Token", token);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            request.Content = new StreamContent(contentStream);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            try
            {
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StorageService] Upload failed for {objectKey}: {ex.Message}");
                return false;
            }
        }
    }
}
