using Microsoft.AspNetCore.Mvc;
using DotnetCrawler.Data;
using Microsoft.EntityFrameworkCore;
using DotnetCrawler.Entities;

namespace DotnetCrawler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public SettingsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("storage")]
        public async Task<IActionResult> GetStorageSettings()
        {
            var url = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "STORAGE_API_URL");
            var bucket = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "STORAGE_BUCKET");
            var token = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "STORAGE_TOKEN");

            return Ok(new
            {
                StorageApiUrl = url?.Value ?? "",
                StorageBucket = bucket?.Value ?? "",
                StorageToken = token?.Value ?? ""
            });
        }

        public class StorageSettingsDto
        {
            public string StorageApiUrl { get; set; } = string.Empty;
            public string StorageBucket { get; set; } = string.Empty;
            public string StorageToken { get; set; } = string.Empty;
        }

        [HttpPut("storage")]
        public async Task<IActionResult> UpdateStorageSettings([FromBody] StorageSettingsDto dto)
        {
            await UpdateSetting("STORAGE_API_URL", dto.StorageApiUrl);
            await UpdateSetting("STORAGE_BUCKET", dto.StorageBucket);
            await UpdateSetting("STORAGE_TOKEN", dto.StorageToken);

            await _dbContext.SaveChangesAsync();
            return Ok(new { Message = "Cập nhật thành công." });
        }

        private async Task UpdateSetting(string key, string value)
        {
            var setting = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                _dbContext.SystemSettings.Add(new SystemSetting { Key = key, Value = value });
            }
            else
            {
                setting.Value = value;
            }
        }
    }
}
