using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtualPatDiagnosis.Models
{
    public class ScoringReportItemVM
    {
        public int No { get; set; }
        public string Name { get; set; } = "";
        public string Level { get; set; } = "";
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public string MatchedPhrases { get; set; } = "";
        public string Explanation { get; set; } = "";
        public List<ScoringRuleViewModel> ScoringRules { get; set; } = new();
    }

    public class ScoringReportVM
    {
        // Header
        public string DisplayName { get; set; } = "";
        public string Title { get; set; } = "";

        // 成績
        public int TotalScore { get; set; }
        public int TotalMax { get; set; }
        public int PassScore { get; set; }
        public bool Pass => TotalScore >= PassScore;

        // 作答時間
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int UsedMinutes
        {
            get
            {
                if (StartedAt.HasValue && EndedAt.HasValue)
                    return (int)Math.Round((EndAtLocal - StartAtLocal).TotalMinutes);
                return 0;
            }
        }
        private DateTime StartAtLocal => StartedAt!.Value;
        private DateTime EndAtLocal => EndedAt!.Value;

        // 題目明細
        public List<ScoringReportItemVM> Items { get; set; } = new();

        // 完成度計算
        public int CompletedCount => Items.Count(x => x.Score > 0);
        public int TotalItems => Items.Count;
        public double Percent => TotalItems == 0 ? 0 : (double)CompletedCount / TotalItems;

        // Donut 幾何
        public double Radius { get; set; } = 36;
        public double Circumference => 2 * Math.PI * Radius;
        public double DashOffset => Circumference * (1 - Percent);

        public static bool IsDone(string level) =>
            level.Equals("done", StringComparison.OrdinalIgnoreCase) || level.Contains("完全");
        public static bool IsPartial(string level) =>
            level.Equals("partial", StringComparison.OrdinalIgnoreCase) || level.Contains("部分");
        public static bool IsMiss(string level) =>
            level.Equals("miss", StringComparison.OrdinalIgnoreCase) || level.Contains("未");
    }
}
