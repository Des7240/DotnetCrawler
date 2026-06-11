using Microsoft.AspNetCore.Mvc;
using DotnetCrawler.Services;

namespace DotnetCrawler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly DataImporterService _importer;

        public ImportController(DataImporterService importer)
        {
            _importer = importer;
        }

        [HttpPost]
        public IActionResult StartImport([FromBody] ImportRequest request)
        {
            if (string.IsNullOrEmpty(request.SourcePath))
            {
                return BadRequest("SourcePath is required.");
            }

            // Start in background
            _ = _importer.StartImportAsync(request);

            return Ok(new { Message = "Import started in background." });
        }

        [HttpGet("progress")]
        public IActionResult GetProgress()
        {
            return Ok(_importer.Progress);
        }
    }
}
