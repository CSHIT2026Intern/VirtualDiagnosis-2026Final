using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VirtualPatDiagnosis.Models
{
    public class ChecklistScore
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentResponseId { get; set; }

        [ForeignKey("StudentResponseId")]
        public StudentResponse? StudentResponse { get; set; }

        [Required]
        public int ChecklistItemId { get; set; }

        [ForeignKey("ChecklistItemId")]
        public ChecklistItem? ChecklistItem { get; set; }

        public string? MatchedPhrases { get; set; }
        public string? Level { get; set; }
        public int? Score { get; set; }
        public string? Explanation { get; set; }
    }
}
