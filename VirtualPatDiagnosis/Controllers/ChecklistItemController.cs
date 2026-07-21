using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using VirtualPatDiagnosis.Models;
using System.Security.Claims;
using System.Text.Json;

namespace VirtualPatDiagnosis.Controllers
{
    [Authorize]
    public class ChecklistItemController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChecklistItemController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ChecklistItem/Create?examCaseId=1
        public IActionResult Create(int examCaseId)
        {
            var checklistItem = new ChecklistItem { ExamCaseId = examCaseId };
            return View(checklistItem);
        }

        // POST: ChecklistItem/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChecklistItemAjaxViewModel vm)
        {
            if (ModelState.IsValid)
            {
                var checklistItem = new ChecklistItem
                {
                    ExamCaseId = vm.ExamCaseId,
                    Name = vm.Name,
                    Description = vm.Description,
                    MaxScore = vm.MaxScore,
                    KeyPhrases = vm.KeyPhrases != null ? JsonSerializer.Serialize(vm.KeyPhrases) : "[]",
                    ScoringRules = vm.ScoringRules != null ? JsonSerializer.Serialize(vm.ScoringRules) : "[]"
                };
                _context.ChecklistItems.Add(checklistItem);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "ExamCase", new { id = checklistItem.ExamCaseId });
            }
            return View(vm);
        }

        // GET: ChecklistItem/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.ChecklistItems.FirstOrDefaultAsync(c => c.Id == id);
            if (item == null) return NotFound();

            ViewBag.KeyPhrases = string.IsNullOrEmpty(item.KeyPhrases) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(item.KeyPhrases);
            ViewBag.ScoringRules = string.IsNullOrEmpty(item.ScoringRules) ? new List<ScoringRuleViewModel>() : JsonSerializer.Deserialize<List<ScoringRuleViewModel>>(item.ScoringRules);

            return View(item);
        }

        // POST: ChecklistItem/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ChecklistItem checklistItem, List<string> keyPhrases, List<ScoringRuleViewModel> scoringRules)
        {
            if (id != checklistItem.Id) return NotFound();

            if (ModelState.IsValid)
            {
                checklistItem.KeyPhrases = keyPhrases != null ? JsonSerializer.Serialize(keyPhrases) : "[]";
                checklistItem.ScoringRules = scoringRules != null ? JsonSerializer.Serialize(scoringRules) : "[]";
                _context.Update(checklistItem);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "ExamCase", new { id = checklistItem.ExamCaseId });
            }

            return View(checklistItem);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] ChecklistItemAjaxViewModel item)
        {
            if (item == null)
            {
                return BadRequest(new { success = false, message = "資料格式錯誤，請重新填寫。" });
            }

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                return BadRequest(new { success = false, message = "請輸入檢核項目名稱。" });
            }

            if (item.MaxScore < 0)
            {
                return BadRequest(new { success = false, message = "最大分數不可小於 0。" });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var examCase = await _context.ExamCases
                .FirstOrDefaultAsync(e => e.Id == item.ExamCaseId && e.CreatedByUserId == userId);

            if (examCase == null)
            {
                return Forbid();
            }

            try
            {
                var checklistItem = new ChecklistItem
                {
                    ExamCaseId = item.ExamCaseId,
                    Name = item.Name,
                    Description = item.Description,
                    MaxScore = item.MaxScore,
                    KeyPhrases = item.KeyPhrases != null ? JsonSerializer.Serialize(item.KeyPhrases) : "[]",
                    ScoringRules = item.ScoringRules != null ? JsonSerializer.Serialize(item.ScoringRules) : "[]"
                };

                _context.ChecklistItems.Add(checklistItem);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    itemId = checklistItem.Id,
                    name = checklistItem.Name,
                    maxScore = checklistItem.MaxScore,
                    keywords = item.KeyPhrases,
                    rules = item.ScoringRules
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "儲存失敗：" + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAjax([FromBody] ChecklistItemAjaxViewModel updated)
        {
            var item = await _context.ChecklistItems.FirstOrDefaultAsync(i => i.Id == updated.Id);

            if (item == null)
                return NotFound(new { success = false, message = "找不到項目" });

            item.Name = updated.Name;
            item.Description = updated.Description;
            item.MaxScore = updated.MaxScore;
            item.KeyPhrases = updated.KeyPhrases != null ? JsonSerializer.Serialize(updated.KeyPhrases) : "[]";
            item.ScoringRules = updated.ScoringRules != null ? JsonSerializer.Serialize(updated.ScoringRules) : "[]";

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var item = await _context.ChecklistItems.FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
                return NotFound(new { success = false, message = "找不到檢核項目。" });

            _context.ChecklistItems.Remove(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }

    public class ChecklistItemAjaxViewModel
    {
        public int Id { get; set; }
        public int ExamCaseId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int MaxScore { get; set; }
        public List<KeyPhraseViewModel> KeyPhrases { get; set; }
        public List<ScoringRuleViewModel> ScoringRules { get; set; }
    }
}
