using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VirtualPatDiagnosis.Models;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace VirtualPatDiagnosis.Controllers
{
    public class ScoreController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string? _apiKey;
        private readonly string? _endpoint;
        private readonly string? _deployment;
        private const string ApiVersion = "2024-12-01-preview";

        public ScoreController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _apiKey = config["AzureOpenAI_GPT5:ApiKey"];
            _endpoint = config["AzureOpenAI_GPT5:Endpoint"];
            _deployment = config["AzureOpenAI_GPT5:DeploymentName"];
        }

        public async Task<IActionResult> ExamResultReport(int resultId)
        {
            var result = await _context.ExamResults.FindAsync(resultId);
            if (result == null) return NotFound("找不到成績紀錄");

            var gptItems = JsonSerializer.Deserialize<List<GptScoreItem>>(result.GptResultJson);
            var examCase = await _context.ExamCases.FindAsync(result.ExamCaseId);

            // 取得 ChecklistItem 列表
            var checklistItems = await _context.Set<ChecklistItem>()
                .Where(ci => ci.ExamCaseId == result.ExamCaseId)
                .OrderBy(ci => ci.Id)
                .ToListAsync();

            // 以 ChecklistItem 為主，左連接 gptItems。
            // 優先用 id 比對（GptController 產生的 id 是固定格式 "Q{ChecklistItemId}"，
            // 不受 GPT 是否照抄 name 影響，比較可靠）；
            // 若某筆舊資料的 GptResultJson 是在改用 id 比對之前產生、id 對不起來，
            // 才退回用 name 字串完全相等比對，當作相容舊資料的備援。
            var items = checklistItems.Select((ci, idx) =>
            {
                var gpt = FindGptItemById(gptItems, ci.Id)
                          ?? gptItems.FirstOrDefault(x => x.name == ci.Name);

                return new ScoringReportItemVM
                {
                    No = idx + 1,
                    Name = CombineItemFullName(ci.Name, ci.Description),
                    Level = gpt?.level ?? "未做到",
                    Score = gpt?.score ?? 0,
                    MaxScore = ci.MaxScore,
                    MatchedPhrases = GetKeyPhrasesString(ci.KeyPhrases),
                    Explanation = gpt?.matched_phrases != null ? string.Join(", ", gpt.matched_phrases) : "",
                    ScoringRules = GetScoringRules(ci.ScoringRules)
                };
            }).ToList();

            var vm = new ScoringReportVM
            {
                DisplayName = "考生",
                Title = examCase?.Title ?? "未命名測驗",
                TotalScore = result.TotalScore,
                PassScore = examCase?.PassScore ?? 0,
                StartedAt = result.StartTime,
                EndedAt = result.SubmittedAt,
                Items = items,
                TotalMax = items.Count * 2
            };

            // 自動產生回饋
            var prompt = GenerateFeedbackPrompt(vm, examCase?.Title ?? "", examCase?.PassScore ?? 0, items);
            var summary = await GenerateAiFeedbackAsync(prompt);
            ViewBag.Summary = summary ?? "無法取得回饋";

            // 用時
            var usedMinutes = (int)Math.Round((result.SubmittedAt - result.StartTime).TotalMinutes);
            ViewBag.UsedMinutes = usedMinutes;

            // 測驗時間（教案設定的作答時限，用於正式評分表 PDF 匯出）
            ViewBag.TimeLimit = examCase?.TimeLimit;

            return View("Scoring", vm);
        }

        // GPT 評分項目資料結構（與 gptResultJson 對應）
        public class GptScoreItem
        {
            public string id { get; set; }
            public string name { get; set; }
            public List<string> matched_phrases { get; set; }
            public string level { get; set; }
            public int score { get; set; }
        }

        // 給 GPT 的 prompt 每題摘要
        public class PromptItemRow
        {
            public string Name { get; set; }
            public string Level { get; set; }
            public int Score { get; set; }
            public int MaxScore { get; set; }
            public string MatchedPhrases { get; set; }
            public string Explanation { get; set; }
        }

        // 產生 GPT 回饋 prompt
        private static string GenerateFeedbackPrompt(
            ScoringReportVM vm, string? examTitle, int passScore, List<ScoringReportItemVM> items)
        {
            var checklist = string.Join("\n", items.Select((x, idx) =>
                $"- Q{idx + 1} {x.Name}: level={x.Level}, score={x.Score}/{x.MaxScore}; expect={x.MatchedPhrases}; said={x.Explanation}"));

            return $$"""
你是一位臨床 OSCE 評審。請根據下列題組與學生的檢核表現與實際問答內容，產出「一句簡短明確的總評（最多120字）」：
- 語氣中性、具體，避免洩題或過度提示
- 不提供診斷或處置建議
- 不重述分數與百分比
- 僅依據提供的資訊，不要臆測新內容

題組: "{{examTitle ?? vm.Title}}"
作答時間: {{vm.StartedAt:yyyy/MM/dd HH:mm}} ~ {{vm.EndedAt:yyyy/MM/dd HH:mm}}
及格分: {{passScore}}

各項目與對話摘要：
{{checklist}}

請只輸出以下 JSON 物件（不得包含多餘文字，也不要包在程式碼區塊）：
{
  "summary": "一段簡短總評（最多120字）"
}
""";
        }

        // 呼叫 Azure OpenAI 取得回饋
        private async Task<string?> GenerateAiFeedbackAsync(string prompt, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) ||
                string.IsNullOrWhiteSpace(_endpoint) ||
                string.IsNullOrWhiteSpace(_deployment))
                return null;

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.Add("api-key", _apiKey);

            var body = new
            {
                messages = new object[]
                {
                    new { role = "system", content = "你只允許輸出 JSON 物件：{\"summary\":\"...\"}，不得包含任何多餘文字。" },
                    new { role = "user", content = prompt }
                }
            };

            var url = $"{_endpoint.TrimEnd('/')}/openai/deployments/{_deployment}/chat/completions?api-version={ApiVersion}";
            var req = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var res = await http.PostAsync(url, req, ct);
            var json = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode) return null;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrWhiteSpace(content)) return null;

                // content 應為 JSON；若模型前後多了文字，嘗試擷取第一個 '{' 到最後一個 '}'
                if (!TryReadSummaryFromJson(content!, out var summary))
                {
                    var start = content.IndexOf('{');
                    var end = content.LastIndexOf('}');
                    if (start >= 0 && end >= start)
                    {
                        var sliced = content.Substring(start, end - start + 1);
                        if (!TryReadSummaryFromJson(sliced, out summary))
                            return null;
                    }
                    else
                    {
                        return null;
                    }
                }

                summary = summary!.Trim();
                if (summary.Length > 120) summary = summary.Substring(0, 120);
                return summary;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryReadSummaryFromJson(string json, out string? summary)
        {
            summary = null;
            try
            {
                using var d = JsonDocument.Parse(json);
                if (d.RootElement.TryGetProperty("summary", out var s) &&
                    s.ValueKind == JsonValueKind.String)
                {
                    summary = s.GetString();
                    return !string.IsNullOrWhiteSpace(summary);
                }
            }
            catch { }
            return false;
        }

        // 工具：用 id（格式 "Q{ChecklistItemId}"，由 GptController.BuildChecklistBlock 產生）
        // 反查對應的 GptScoreItem，比對 name 字串穩定，不受 GPT 有沒有照抄項目名稱影響。
        private static GptScoreItem? FindGptItemById(List<GptScoreItem> gptItems, int checklistItemId)
        {
            foreach (var item in gptItems)
            {
                if (TryParseChecklistItemId(item.id, out var id) && id == checklistItemId)
                    return item;
            }
            return null;
        }

        private static bool TryParseChecklistItemId(string? rawId, out int checklistItemId)
        {
            checklistItemId = 0;
            if (string.IsNullOrWhiteSpace(rawId)) return false;

            var trimmed = rawId.Trim();
            if (trimmed.StartsWith("Q", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(1);

            return int.TryParse(trimmed, out checklistItemId);
        }

        // 工具：把 ChecklistItem 的 Name 跟 Description 合併成完整項目名稱。
        // 手動新增的檢核項目（ExamCase/Details 頁面）Name 跟 Description 是分開輸入的欄位，
        // 例如 Name="相關徵候 1 (發燒與感染全身性症狀)"、Description="：是否有發燒、畏寒(發冷)、全身倦怠無力？"，
        // 兩者合起來才是完整題目。Word 匯入的項目通常整段都在 Name 裡、Description 是空的，
        // 這裡也能正確處理（不會多加東西）。
        private static string CombineItemFullName(string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return name;

            var desc = description.Trim();
            // Description 若不是以中英文冒號或其他標點開頭，補一個全形冒號分隔，
            // 避免合併後名稱跟說明黏在一起看不清楚。
            bool startsWithPunctuation = desc.Length > 0 &&
                "：:（(，,。.、－-".IndexOf(desc[0]) >= 0;

            return startsWithPunctuation ? $"{name}{desc}" : $"{name}：{desc}";
        }

        // 工具：解析關鍵詞
        private static string GetKeyPhrasesString(string? keyPhrasesJson)
        {
            if (string.IsNullOrWhiteSpace(keyPhrasesJson)) return "";
            try
            {
                var list = JsonSerializer.Deserialize<List<KeyPhraseViewModel>>(keyPhrasesJson);
                return list != null ? string.Join(", ", list.Select(k => k.phrase)) : "";
            }
            catch { return ""; }
        }

        // 工具：解析評分規則
        private static List<ScoringRuleViewModel> GetScoringRules(string? scoringRulesJson)
        {
            if (string.IsNullOrWhiteSpace(scoringRulesJson)) return new List<ScoringRuleViewModel>();
            try
            {
                return JsonSerializer.Deserialize<List<ScoringRuleViewModel>>(scoringRulesJson) ?? new List<ScoringRuleViewModel>();
            }
            catch { return new List<ScoringRuleViewModel>(); }
        }

        // 轉址
        public IActionResult Index(int resultId) =>
            RedirectToAction(nameof(ExamResultReport), new { resultId });

        public IActionResult Scoring(int resultId) =>
            RedirectToAction(nameof(ExamResultReport), new { resultId });
    }
}