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
        public IActionResult StartCrawl([FromBody] CrawlerRequest request)
        {
            if (request == null || request.Threads == null || !request.Threads.Any())
            {
                return BadRequest(new { status = "error", message = "Dữ liệu không hợp lệ." });
            }

            // Kích hoạt tiến trình chạy ngầm
            _crawlerService.StartCrawlBackground(request);

            return Ok(new { 
                status = "success", 
                message = "Tiến trình Crawl đã được kích hoạt chạy ngầm. Vui lòng theo dõi Console." 
            });
        }
    }
}
