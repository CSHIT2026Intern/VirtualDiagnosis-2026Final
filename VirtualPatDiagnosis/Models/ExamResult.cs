using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VirtualPatDiagnosis.Models
{
    public class ExamResult
    {
        public int Id { get; set; }
        public int ExamCaseId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime StartTime { get; set; }
        public int TotalScore { get; set; }
        public string GptResultJson { get; set; }
    }
}