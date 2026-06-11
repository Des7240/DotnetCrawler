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

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(int key)
        {
            var item = _db.Subjects.FirstOrDefault(s => s.Id == key);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpDelete]
        public IActionResult Delete(int key)
        {
            var item = _db.Subjects.FirstOrDefault(s => s.Id == key);
            if (item == null) return NotFound();

            _db.Subjects.Remove(item);
            _db.SaveChanges();

            return NoContent();
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

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(int key)
        {
            var item = _db.Threads.FirstOrDefault(t => t.Id == key);
            if (item == null) return NotFound();
            return Ok(item);
        }
    }

    public class QuestionsController : ODataController
    {
        private readonly AppDbContext _db;

        public QuestionsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(_db.Questions);
        }

        [HttpGet]
        [EnableQuery]
        public IActionResult Get(int key)
        {
            var item = _db.Questions.Include(q => q.Comments).FirstOrDefault(q => q.Id == key);
            if (item == null) return NotFound();
            return Ok(item);
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
