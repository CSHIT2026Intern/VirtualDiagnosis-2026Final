namespace VirtualPatDiagnosis.Models
{
    public class KeyPhraseViewModel
    {
        public string phrase { get; set; }
    }

    public class ScoringRuleViewModel
    {
        public string Level { get; set; }
        public int Score { get; set; }
        public string? Description { get; set; }
    }
}