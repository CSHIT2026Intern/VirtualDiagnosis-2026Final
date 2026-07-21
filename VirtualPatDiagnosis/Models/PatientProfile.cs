using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace VirtualPatDiagnosis.Models
{
    public class PatientProfile
    {
        public int Id { get; set; }

        [Required]
        public int ExamCaseId { get; set; }

        [ForeignKey("ExamCaseId")]
        public ExamCase? ExamCase { get; set; }

        public string? Complaint { get; set; }
        public string? CurrentHistory { get; set; }
        public string? PastHistory { get; set; }
        public string? FamilyHistory { get; set; }
        public string? DrugHistory { get; set; }
        public string? RestrictionRules { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? QA { get; set; }

        // 回答者：由誰扮演這個病人的角色回答問診。
        // 例如："病人本人"、"病人母親"、"病人父親"。
        // 資料庫尚未匯入這個欄位時會是 null，程式端會預設當作「病人本人」。
        public string? Respondent { get; set; }
    }
}