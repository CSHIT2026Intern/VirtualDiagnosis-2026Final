using System.Text.RegularExpressions;
using VirtualPatDiagnosis.Models;
using System.Collections.Generic;

namespace VirtualPatDiagnosis.Helpers
{
    public static class DocumentParserHelper
    {
        private static readonly string[] AllHeadings = new[]
        {
            "[Guidelines]", "[Checklists]", "[Items]", "[Patient]",
            "教案名稱", "案例名稱", "教案標題", "題組名稱", "說明", "教案說明", "案例說明",
            "背景資料", "測驗主題",
            "測驗時間", "測驗時長", "作答時間", "時間限制", "通過分數", "及格分數", "及格標準",
            "姓名", "病人名稱", "病人姓名", "年齡", "歲數", "主訴", "主要問題",
            "主要臨床症狀", "臨床症狀", "現在病史", "現病史",
            "過去病史", "過去史", "家族病史", "家族史",
            "藥物史", "藥物過敏史", "過敏史", "其他病史",
            "限制條件", "QA", "應答者", "扮演身分", "扮演者",
            "評分項目", "檢核項目", "評量項目", "評分標準", "項目名稱", "完全做到", "完整達成", "部分做到", "部分達成", "沒有做到", "未做到", "未達成", "關鍵詞", "關鍵字", "關鍵資訊"
        };

        public sealed class ExamCaseInfo
        {
            public string? Title { get; init; }
            public string? Description { get; init; }
            public int? TimeLimit { get; init; }
            public int? PassScore { get; init; }
        }

        /// <summary>
        /// PDF 文字層常會把換行轉成空白或使用不同的換行字元。先統一文字，
        /// 讓 Word 與 PDF 都能使用相同的欄位解析規則。
        /// </summary>
        public static string NormalizeDocumentText(string? documentText)
        {
            if (string.IsNullOrWhiteSpace(documentText)) return string.Empty;

            var text = documentText
                .Replace('\u00A0', ' ')
                .Replace('\r', '\n');

            text = Regex.Replace(text, @"\n{2,}", "\n");
            text = Regex.Replace(text, @"[ \t]+", " ");
            return text.Trim();
        }

        /// <summary>
        /// 從 fullText 裡找出 keywords 對應的欄位值。
        ///
        /// 🎯 修正說明（原本的 bug）：
        /// 教案 docx 常見寫法會有一行「主訴及病史相關」這種小節標題（沒有冒號），
        /// 下一行才是真正的欄位「主訴：兩週前搬貨閃到腰...」。
        /// 原本的邏輯是「找到關鍵字就直接抓值抓到下一個標題字為止」，
        /// 結果第一次抓到「主訴」是抓在標題行「主訴及病史相關」裡面，
        /// 往後找值時又馬上撞到下一行真正的「主訴：」這個標題字而提前停止，
        /// 導致抓出來的值變成中間那段無意義的「及病史相關」，
        /// 而不是真正的主訴內容——這是空欄位以外，另一種更難被發現的資料錯誤。
        ///
        /// 修正做法：優先比對「關鍵字後面緊接著冒號」的出現位置（真正的欄位一定有冒號），
        /// 找不到才退回原本「冒號可有可無」的寬鬆比對，維持對舊格式的相容性。
        /// </summary>
        private static string ExtractValue(string fullText, params string[] keywords)
        {
            string searchKeys = string.Join("|", keywords.Select(Regex.Escape));
            string stopKeys = string.Join("|", AllHeadings.Select(Regex.Escape));

            string strictPattern = $@"(?:{searchKeys})\s*[:：]\s*(?<value>.*?)(?=\s*(?:{stopKeys})\s*[:：]|\n\s*(?:{stopKeys})|$)";
            Match match = Regex.Match(fullText, strictPattern, RegexOptions.Singleline);

            if (!match.Success)
            {
                string loosePattern = $@"(?:{searchKeys})\s*[:：]?\s*(?<value>.*?)(?=\s*(?:{stopKeys})\s*[:：]|\n\s*(?:{stopKeys})|$)";
                match = Regex.Match(fullText, loosePattern, RegexOptions.Singleline);
            }

            if (match.Success)
            {
                string result = match.Groups["value"].Value.Trim();

                foreach (var heading in AllHeadings)
                {
                    if (result.EndsWith(heading))
                    {
                        result = result.Substring(0, result.Length - heading.Length).Trim();
                        break;
                    }
                }
                return result;
            }
            return null!;
        }

        private static int? ExtractInteger(string fullText, params string[] keywords)
        {
            var value = ExtractValue(fullText, keywords);
            var match = Regex.Match(value ?? string.Empty, @"\d+");
            return match.Success && int.TryParse(match.Value, out var number) ? number : null;
        }

        /// <summary>讀取教案基本資料，支援常見的不同欄位名稱。</summary>
        public static ExamCaseInfo ParseExamCaseInfo(string documentText)
        {
            documentText = NormalizeDocumentText(documentText);
            return new()
        {
            Title = ExtractValue(documentText, "教案名稱", "案例名稱", "教案標題", "題組名稱"),
            Description = ExtractValue(documentText, "教案說明", "案例說明", "說明", "情境說明"),
            TimeLimit = ExtractInteger(documentText, "測驗時間", "測驗時長", "作答時間", "時間限制"),
            PassScore = ExtractInteger(documentText, "通過分數", "及格分數", "及格標準")
        };
        }

        /// <summary>
        /// </summary>
        public static PatientProfile ParsePatientProfile(string documentText)
        {
            documentText = NormalizeDocumentText(documentText);
            if (string.IsNullOrWhiteSpace(documentText)) return new PatientProfile();

            string patientText = documentText;
            int patientIndex = FindFirstIndex(documentText, "[Patient]", "病人資料", "個案資料", "病患資料");
            if (patientIndex >= 0)
            {
                patientText = documentText.Substring(patientIndex);
            }

            var profile = new PatientProfile();

            profile.Name = ExtractValue(patientText, "姓名", "病人名稱", "病人姓名", "個案姓名");
            profile.Complaint = ExtractValue(patientText, "主訴", "主要問題", "就診原因");
            profile.CurrentHistory = ExtractValue(patientText, "現在病史", "現病史", "病史摘要");
            profile.PastHistory = ExtractValue(patientText, "過去病史", "過去史");
            profile.FamilyHistory = ExtractValue(patientText, "家族病史", "家族史");
            profile.DrugHistory = ExtractValue(patientText, "藥物史", "藥物過敏史", "過敏史");
            profile.RestrictionRules = ExtractValue(patientText, "限制條件", "限制");
            profile.QA = ExtractValue(patientText, "QA", "Q&A");
            profile.Respondent = ExtractValue(patientText, "扮演身分", "應答者", "扮演者");

            // 擷取年齡
            string ageStr = ExtractValue(patientText, "年齡", "歲數", "年紀");
            if (int.TryParse(ageStr, out int age))
            {
                profile.Age = age;
            }
            else if (!string.IsNullOrWhiteSpace(ageStr))
            {
                Match numberMatch = Regex.Match(ageStr, @"\d+");
                profile.Age = (numberMatch.Success && int.TryParse(numberMatch.Value, out int extAge)) ? extAge : 0;
            }

            return profile;
        }

        /// <summary>
        /// </summary>
        public static List<ChecklistItem> ParseChecklists(string documentText)
        {
            var items = new List<ChecklistItem>();
            documentText = NormalizeDocumentText(documentText);

            int startIndex = FindFirstIndex(documentText, "[Items]", "評分項目", "檢核項目", "評量項目", "評分標準");
            if (startIndex < 0) return items;

            int endIndex = FindFirstIndex(documentText, "[Patient]", "病人資料", "個案資料", "病患資料");
            string itemsText = documentText;

            if (endIndex != -1 && endIndex > startIndex)
            {
                itemsText = documentText.Substring(startIndex, endIndex - startIndex);
            }
            else
            {
                itemsText = documentText.Substring(startIndex);
            }

            // PDF 匯出時常遺失換行，因此不要求編號一定要在每一行的開頭。
            // 同時略過「評分項目（每個項目…）」這種說明列，避免它被誤存成第一題。
            string[] blocks = Regex.Split(itemsText, @"(?=\[\s*\d+\s*\]|\d+\s*[\.、．]|[一二三四五六七八九十]+\s*[、．])");

            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block)) continue;
                if (!Regex.IsMatch(block, @"^\s*(?:\[\s*\d+\s*\]|\d+\s*[\.、．]|[一二三四五六七八九十]+\s*[、．])")) continue;

                var item = new ChecklistItem();
                item.Name = ExtractValue(block, "項目名稱", "檢核內容", "評分內容", "評分項目");

                // 如果這個區塊連項目名稱都沒有，代表是無效區塊，直接跳過
                if (string.IsNullOrEmpty(item.Name)) continue;

                item.KeyPhrases = ExtractValue(block, "關鍵詞", "關鍵字", "關鍵資訊");

                // 分別抓取三個計分規則
                string perfect = ExtractValue(block, "完全做到", "完整達成");
                string partial = ExtractValue(block, "部分做到", "部分達成");
                string none = ExtractValue(block, "沒有做到", "未做到", "未達成");

                item.ScoringRules = $"完全做到：{perfect}\n部分做到：{partial}\n沒有做到：{none}";


                item.MaxScore = 2;

                items.Add(item);
            }

            return items;
        }

        private static int FindFirstIndex(string text, params string[] markers)
        {
            var positions = markers
                .Select(marker => text.IndexOf(marker, StringComparison.OrdinalIgnoreCase))
                .Where(position => position >= 0);
            return positions.DefaultIfEmpty(-1).Min();
        }
    }
}
