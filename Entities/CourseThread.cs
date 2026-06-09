using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotnetCrawler.Entities
{
    public class CourseThread
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty; // pe, fe, other

        [Required]
        [MaxLength(1000)]
        public string Path { get; set; } = string.Empty; // e.g. /threads/pru...

        [ForeignKey("Subject")]
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<ThreadFile> Files { get; set; } = new List<ThreadFile>();
    }
}
