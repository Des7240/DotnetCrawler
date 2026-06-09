using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotnetCrawler.Entities
{
    public class ThreadFile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Url { get; set; } = string.Empty;

        [ForeignKey("CourseThread")]
        public int CourseThreadId { get; set; }
        public CourseThread? CourseThread { get; set; }
    }
}
