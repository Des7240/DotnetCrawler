using System.Collections.Generic;

namespace DotnetCrawler.Entities
{
    public class QuestionDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? BestAnswer { get; set; }
        public string? GeminiAnswer { get; set; }
        public int CourseThreadId { get; set; }
        public CourseThread? CourseThread { get; set; }
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
