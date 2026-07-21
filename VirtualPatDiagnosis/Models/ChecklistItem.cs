using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VirtualPatDiagnosis.Models;

namespace VirtualPatDiagnosis.Models
{
    public class ChecklistItem
    {
        public int Id { get; set; }

        [Required]
        public int ExamCaseId { get; set; }

        [ForeignKey("ExamCaseId")]
        public ExamCase? ExamCase { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = "";

        public string? Description { get; set; }

        [Required]
        public int MaxScore { get; set; }

        public string? KeyPhrases { get; set; }
        public string? ScoringRules { get; set; }
    }
}
