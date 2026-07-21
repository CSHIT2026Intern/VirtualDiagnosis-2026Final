using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VirtualPatDiagnosis.Models;
using System.Text.Json;
using System.Linq;
using System.Text.RegularExpressions;

namespace VirtualPatDiagnosis.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        // 透過建構子注入 Logger 與資料庫上下文 ApplicationDbContext
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // 🎯 核心修改：接收從讀題頁面（ReadQuestion）傳過來的 examCaseId
        public IActionResult Index(int examCaseId)
        {
            // 從資料庫撈出對應的教案
            var dbCase = _context.ExamCases.FirstOrDefault(c => c.Id == examCaseId);

            if (dbCase == null)
            {
                return NotFound("找不到該問診教案");
            }

            // 封裝強型別 QuestionViewModel
            var vm = new QuestionViewModel
            {
                // 完美對應你所要求的資料庫欄位
                ExamCaseId = dbCase.Id,
                Background = dbCase.Description,
                // 防止 int? 轉 string 出錯，並自動加上「分鐘」字樣，若為空則預設 8 分鐘
                Time = dbCase.TimeLimit.HasValue ? $"{dbCase.TimeLimit}分鐘" : "8分鐘",
                
                // Topic 目前暫無對應，給予預設文字
                Topic = "● 完整詢問病史<br/>● 請根據個案狀況進行評估"
            };

            // 🎯 依教案「背景資料」文字（例如「30歲女性」「21歲男性」）自動解析年齡與性別，
            // 並依此配對對應插圖（男童 boy、女童 patient.PNG媽媽抱著、成年 youngman/youngwoman、年長 oldman/oldwoman）。
            // 解析不到年齡/性別時，一律 fallback 回通用預設的 patient.PNG，絕對不會顯示空白。
            var (patientAge, patientIsMale) = TryParseAgeGender(dbCase.Description);
            ViewBag.PatientImage = DeterminePatientImage(dbCase.Description);

            // 🎯 動態狀態文字：胸痛教案優先顯示痛苦手捂胸口；未滿 18 歲的女童（對應 patient.PNG）顯示被媽媽抱著；其餘預設坐在診間椅子上
            if (dbCase.Id == 99 || dbCase.Title.Contains("胸痛"))
            {
                ViewBag.PatientStatusText = "（病人痛苦地手捂著胸口）";
            }
            else if (patientAge.HasValue && patientAge.Value < 18 && patientIsMale == false)
            {
                ViewBag.PatientStatusText = "（被媽媽抱在身上）";
            }
            else
            {
                // 🆕 老師端未來新匯入的其他所有新教案，只要背景資料有「XX歲男性/女性」就會自動配圖
                ViewBag.PatientStatusText = "（病人靜坐在診間椅子上）";
            }

            ViewBag.Title = "模擬診間互動頁";
            return View(vm);
        }

        // 🎯 從教案背景資料中解析「XX歲男性/女性」這種固定格式的年齡與性別
        private static readonly Regex AgeGenderRegex = new Regex(@"(\d+)\s*歲\s*(男|女)", RegexOptions.Compiled);

        // 回傳 (年齡, 是否為男性)；解析失敗時兩者皆為 null
        private static (int? age, bool? isMale) TryParseAgeGender(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return (null, null);
            var match = AgeGenderRegex.Match(description);
            if (!match.Success) return (null, null);
            int age = int.Parse(match.Groups[1].Value);
            bool isMale = match.Groups[2].Value == "男";
            return (age, isMale);
        }

        // 🎯 依年齡層 + 性別自動配對插圖：
        // 未滿 18 歲的男童 → boy.png（單獨站著）
        // 未滿 18 歲的女童 → patient.png（媽媽抱著女兒，兒科案例專用圖）
        // 18~64 歲   → 成年 (youngman / youngwoman)
        // 65 歲以上  → 年長 (oldman / oldwoman)
        // 解析失敗（背景資料沒有「XX歲男性/女性」格式）→ 通用預設 patient.PNG
        private static string DeterminePatientImage(string? description)
        {
            var (age, isMale) = TryParseAgeGender(description);

            if (age is null || isMale is null)
                return "/images/patient.png";

            if (age < 18)
                return isMale.Value ? "/images/boy.png" : "/images/patient.png"; // 女童一律用媽媽抱女兒的兒科圖

            if (age < 65)
                return isMale.Value ? "/images/youngman.png" : "/images/youngwoman.png";

            return isMale.Value ? "/images/oldman.png" : "/images/oldwoman.png";
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // 💾 原本保留：處理測驗結果提交並存入資料庫
        [HttpPost]
        public IActionResult SubmitExam(int examCaseId, string? gptResult, string? startTime)
        {
            List<GptScoreItem> gptItems = new();
            if (!string.IsNullOrWhiteSpace(gptResult))
            {
                try
                {
                    gptItems = JsonSerializer.Deserialize<List<GptScoreItem>>(gptResult) ?? new();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "無法解析教案 {ExamCaseId} 的評分資料。", examCaseId);
                    return BadRequest("評分資料格式不正確，請留在目前頁面後重新送出。");
                }
            }

            int totalScore = gptItems
                .Where(x => !string.IsNullOrEmpty(x.id))
                .Sum(x => x.score);

            DateTime startDt = DateTime.TryParse(startTime, out var dt) ? dt : DateTime.Now;

            var result = new ExamResult
            {
                ExamCaseId = examCaseId,
                StartTime = startDt,
                SubmittedAt = DateTime.Now,
                TotalScore = totalScore,
                GptResultJson = JsonSerializer.Serialize(gptItems)
            };
            _context.ExamResults.Add(result);
            _context.SaveChanges();

            return Json(new { redirectUrl = Url.Action("ExamResultReport", "Score", new { resultId = result.Id }) });
        }

        // 🗂️ 原本保留：GPT 評分項目對應的模型類別
        public class GptScoreItem
        {
            public string id { get; set; }
            public string name { get; set; }
            public List<string> matched_phrases { get; set; }
            public string level { get; set; }
            public int score { get; set; }
        }
    }
}
