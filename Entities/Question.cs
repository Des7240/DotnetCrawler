using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotnetCrawler.Entities
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(1000)]
        public string ImageUrl { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? BestAnswer { get; set; }

        [Column(TypeName = "TEXT")]
        public string? GeminiAnswer { get; set; }

        public int Votes { get; set; } = 0;

        [ForeignKey("CourseThread")]
        public int CourseThreadId { get; set; }
        public CourseThread? CourseThread { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
