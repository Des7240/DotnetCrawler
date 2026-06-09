using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotnetCrawler.Data;

namespace DotnetCrawler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public CourseController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpGet("subjects")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> GetSubjects()
        {
            var subjects = await _db.Subjects.ToListAsync();
            return Ok(subjects);
        }

        [HttpGet("threads")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> GetThreads([FromQuery] int subjectId)
        {
            var threads = await _db.Threads
                .Where(t => t.SubjectId == subjectId)
                .ToListAsync();
            return Ok(threads);
        }

        [HttpGet("questions")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> GetQuestions([FromQuery] int threadId)
        {
            var storageUrl = _config["STORAGE_API_URL"]?.TrimEnd('/') ?? "";
            var bucket = _config["STORAGE_BUCKET"] ?? "";
            var baseUrl = $"{storageUrl}/{bucket}";

            var questions = await _db.Questions
                .Include(q => q.Comments)
                .Include(q => q.CourseThread)
                .Where(q => q.CourseThreadId == threadId)
                .Select(q => new
                {
                    q.Id,
                    image = string.IsNullOrEmpty(storageUrl) ? q.ImageUrl : $"{baseUrl}/{q.CourseThread!.Path}/{q.ImageUrl}",
                    best_answer = q.BestAnswer,
                    gemini_answer = q.GeminiAnswer,
                    comments = q.Comments.Select(c => new { text = c.Text, count = c.Votes }).ToList()
                })
                .ToListAsync();
            
            return Ok(questions);
        }

        [HttpGet("files")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> GetFiles([FromQuery] int threadId)
        {
            var files = await _db.ThreadFiles
                .Where(f => f.CourseThreadId == threadId)
                .ToListAsync();
            return Ok(files);
        }
    }
}
