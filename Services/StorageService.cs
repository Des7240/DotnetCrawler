using System.Net.Http.Headers;

namespace DotnetCrawler.Services
{
    public class StorageService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public StorageService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<bool> UploadImageStreamAsync(string objectKey, Stream contentStream, string contentType)
        {
            var apiUrl = _config["STORAGE_API_URL"]; // e.g. http://localhost:5033/api/storage
            var bucket = _config["STORAGE_BUCKET"];
            var token = _config["STORAGE_TOKEN"];

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
