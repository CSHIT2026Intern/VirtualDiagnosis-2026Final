namespace VirtualPatDiagnosis.Models
{
    public class QuestionViewModel
    {
        public string Guideline { get; set; }
        public string Background { get; set; }
        public string Topic { get; set; }
        public string Time { get; set; }
        public int ExamCaseId { get; set; }
    }
}