using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotnetCrawler.Entities
{
    public class CommentVote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public AppUser? User { get; set; }

        [Required]
        public int CommentId { get; set; }

        [ForeignKey("CommentId")]
        public Comment? Comment { get; set; }

        // e.g., 1 for upvote, -1 for downvote
        public int Value { get; set; }
    }
}
