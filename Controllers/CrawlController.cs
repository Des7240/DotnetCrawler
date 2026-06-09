using Microsoft.AspNetCore.Mvc;
using DotnetCrawler.Services;

namespace DotnetCrawler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CrawlController : ControllerBase
    {
        private readonly CrawlerService _crawlerService;

        public CrawlController(CrawlerService crawlerService)
        {
            _crawlerService = crawlerService;
        }

        [HttpPost]
        public async Task<IActionResult> StartCrawl([FromBody] CrawlerRequest request)
        {
            if (request == null || request.Threads == null || !request.Threads.Any())
            {
                return BadRequest(new { status = "error", message = "Dữ liệu không hợp lệ." });
            }

            try
            {
                await _crawlerService.StartCrawlAsync(request);
                return Ok(new { 
                    status = "success", 
                    message = "Đã hoàn tất Crawl và lưu dữ liệu thành công." 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }
    }
}
