using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotnetCrawler.Data;
using DotnetCrawler.Entities;
using System.Threading.Tasks;

namespace DotnetCrawler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RatingController : ControllerBase
    {
        private readonly AppDbContext _db;

        public RatingController(AppDbContext db)
        {
            _db = db;
        }

        public class RateRequest
        {
            public int UserId { get; set; }
            public int Value { get; set; } // 1 for upvote, -1 for downvote, 0 to remove
        }

        [HttpPost("question/{id}")]
        public async Task<IActionResult> RateQuestion(int id, [FromBody] RateRequest req)
        {
            var question = await _db.Questions.FindAsync(id);
            if (question == null) return NotFound("Question not found");

            var vote = await _db.QuestionVotes.FirstOrDefaultAsync(v => v.QuestionId == id && v.UserId == req.UserId);
            
            if (vote == null)
            {
                if (req.Value == 0) return Ok(question);
                vote = new QuestionVote { QuestionId = id, UserId = req.UserId, Value = req.Value };
                _db.QuestionVotes.Add(vote);
                question.Votes += req.Value;
            }
            else
            {
                // remove old vote from count
                question.Votes -= vote.Value;
                
                if (req.Value == 0)
                {
                    _db.QuestionVotes.Remove(vote);
                }
                else
                {
                    vote.Value = req.Value;
                    question.Votes += req.Value;
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { question.Id, question.Votes });
        }

        [HttpPost("comment/{id}")]
        public async Task<IActionResult> RateComment(int id, [FromBody] RateRequest req)
        {
            var comment = await _db.Comments.FindAsync(id);
            if (comment == null) return NotFound("Comment not found");

            var vote = await _db.CommentVotes.FirstOrDefaultAsync(v => v.CommentId == id && v.UserId == req.UserId);
            
            if (vote == null)
            {
                if (req.Value == 0) return Ok(comment);
                vote = new CommentVote { CommentId = id, UserId = req.UserId, Value = req.Value };
                _db.CommentVotes.Add(vote);
                comment.Votes += req.Value;
            }
            else
            {
                comment.Votes -= vote.Value;
                
                if (req.Value == 0)
                {
                    _db.CommentVotes.Remove(vote);
                }
                else
                {
                    vote.Value = req.Value;
                    comment.Votes += req.Value;
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { comment.Id, comment.Votes });
        }
    }
}
