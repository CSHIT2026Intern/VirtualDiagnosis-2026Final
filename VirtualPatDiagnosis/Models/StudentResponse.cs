using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VirtualPatDiagnosis.Models
{
    public class StudentResponse
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExamCaseId { get; set; }

        [ForeignKey("ExamCaseId")]
        public ExamCase? ExamCase { get; set; }

        [Required]
        public int StudentUserId { get; set; }

        [ForeignKey("StudentUserId")]
        public UserAccount? StudentUser { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? FinalScore { get; set; }
        public string? Summary { get; set; }
    }
}