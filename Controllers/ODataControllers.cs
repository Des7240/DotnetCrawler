using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using DotnetCrawler.Data;
using DotnetCrawler.Entities;

namespace DotnetCrawler.Controllers
{
    public class SubjectsController : ODataController
    {
        private readonly AppDbContext _db;

        public SubjectsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(_db.Subjects);
        }
    }

    public class CourseThreadsController : ODataController
    {
        private readonly AppDbContext _db;

        public CourseThreadsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(_db.Threads);
        }
    }

    public class QuestionsController : ODataController
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public QuestionsController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get()
        {
            var storageUrl = _config["STORAGE_API_URL"]?.TrimEnd('/') ?? "";
            var bucket = _config["STORAGE_BUCKET"] ?? "";
            var baseUrl = $"{storageUrl}/{bucket}";

            var query = _db.Questions
                .Select(q => new QuestionDto
                {
                    Id = q.Id,
                    CourseThreadId = q.CourseThreadId,
                    BestAnswer = q.BestAnswer,
                    GeminiAnswer = q.GeminiAnswer,
                    ImageUrl = string.IsNullOrEmpty(storageUrl) ? q.ImageUrl : $"{baseUrl}/{q.CourseThread!.Path}/{q.ImageUrl}",
                    Comments = q.Comments
                });

            return Ok(query);
        }
    }

    public class ThreadFilesController : ODataController
    {
        private readonly AppDbContext _db;

        public ThreadFilesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(_db.ThreadFiles);
        }
    }
}
