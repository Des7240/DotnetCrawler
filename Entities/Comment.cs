using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotnetCrawler.Entities
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "TEXT")]
        public string Text { get; set; } = string.Empty;

        public int Votes { get; set; } = 0;

        [ForeignKey("Question")]
        public int QuestionId { get; set; }
        public Question? Question { get; set; }
    }
}
