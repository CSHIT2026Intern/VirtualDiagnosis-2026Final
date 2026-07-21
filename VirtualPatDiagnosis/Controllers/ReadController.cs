using Microsoft.AspNetCore.Mvc;
using VirtualPatDiagnosis.Models;
using System.Linq;

namespace VirtualPatDiagnosis.Controllers
{
    public class ReadController : Controller
    {
        private readonly ApplicationDbContext _context;

        // 透過相依性注入（DI）引入資料庫內容
        public ReadController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. 動態從資料庫撈出所有可選的教案
        public IActionResult CaseSelect()
        {
            // 撈出資料庫所有的教案，並傳給 View
            var cases = _context.ExamCases.ToList(); // 假設你的資料表實體叫做 ExamCases
            return View(cases);
        }

        // 2. 依據傳入的 caseId 動態撈出對應的指引與資訊
        public IActionResult ReadQuestion(int caseId)
        {
            // 從資料庫找出這筆教案，如果找不到就給 null
            var dbCase = _context.ExamCases.FirstOrDefault(c => c.Id == caseId);

            if (dbCase == null)
            {
                return NotFound("找不到該教案案例");
            }

            var vm = new QuestionViewModel
            {
                // 對應你要求的資料庫欄位
                ExamCaseId = dbCase.Id,
                Background = dbCase.Description,
                Time = dbCase.TimeLimit.HasValue ? $"{dbCase.TimeLimit}分鐘" : "8分鐘",

                
                // 暫時維持原本的設定
                Guideline = $"一、考生指引（{dbCase.Title}）",
                Topic = "● 完整詢問病史<br/>● 請根據個案狀況進行評估" 
            };

            return View(vm);
        }
    }
}
