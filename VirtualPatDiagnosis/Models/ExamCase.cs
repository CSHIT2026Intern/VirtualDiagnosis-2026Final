using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VirtualPatDiagnosis.Models
{
    public class ExamCase
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        public string? Description { get; set; }

        [Required]
        public int PassScore { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public UserAccount? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? TimeLimit { get; set; }

        public List<PatientProfile> PatientProfiles { get; set; } = new();
        public List<ChecklistItem> ChecklistItems { get; set; } = new();
    }
}
