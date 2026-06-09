using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DotnetCrawler.Entities
{
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        public ICollection<CourseThread> Threads { get; set; } = new List<CourseThread>();
    }
}
