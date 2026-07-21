using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VirtualPatDiagnosis.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using System.IO;
using System.Text;
using System.Text.Json;
using VirtualPatDiagnosis.Helpers;

namespace VirtualPatDiagnosis.Controllers
{
    [Authorize]
    public class ExamCaseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExamCaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /ExamCase/
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var examCases = await _context.ExamCases
                .Where(e => e.CreatedByUserId == userId)
                .Include(e => e.CreatedByUser)
                .ToListAsync();

            return View(examCases);
        }


        // GET: /ExamCase/Create
        public IActionResult AddQ()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var existingCases = _context.ExamCases
                .Include(e => e.CreatedByUser)
                .Where(e => e.CreatedByUserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .ToList();

            ViewBag.ExistingCases = existingCases;
            return View("AddQ");
        }

        // POST: /ExamCase/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamCase examCase)
        {
            if (ModelState.IsValid)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized();
                }
                examCase.CreatedByUserId = int.Parse(userIdClaim.Value);

                examCase.CreatedAt = DateTime.Now;

                _context.Add(examCase);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(examCase);
        }

        // GET: /ExamCase/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var examCase = await _context.ExamCases.FindAsync(id);
            if (examCase == null) return NotFound();
            return View(examCase);
        }

        // POST: /ExamCase/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ExamCase examCase)
        {
            if (id != examCase.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingExamCase = await _context.ExamCases.FindAsync(id);
                    if (existingExamCase == null) return NotFound();

                    existingExamCase.Title = examCase.Title;
                    existingExamCase.Description = examCase.Description;
                    existingExamCase.PassScore = examCase.PassScore;
                    existingExamCase.TimeLimit = examCase.TimeLimit;

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ExamCases.Any(e => e.Id == id))
                        return NotFound();
                    else
                        throw;
                }
            }
            return View(examCase);
        }

        // GET: /ExamCase/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var examCase = await _context.ExamCases
                .Include(e => e.PatientProfiles)
                .Include(e => e.ChecklistItems)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (examCase != null)
            {
                _context.ChecklistItems.RemoveRange(examCase.ChecklistItems);
                _context.PatientProfiles.RemoveRange(examCase.PatientProfiles);
                _context.ExamCases.Remove(examCase);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // POST: /ExamCase/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var examCase = await _context.ExamCases
                .Include(e => e.PatientProfiles)
                .Include(e => e.ChecklistItems)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (examCase != null)
            {
                _context.ChecklistItems.RemoveRange(examCase.ChecklistItems);
                _context.PatientProfiles.RemoveRange(examCase.PatientProfiles);
                _context.ExamCases.Remove(examCase);
                await _context.SaveChangesAsync();
            }

            // 回傳 JSON 給 AJAX
            return Json(new { success = true });
        }


        public async Task<IActionResult> Details(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var examCase = await _context.ExamCases
                .Include(e => e.CreatedByUser)
                .Include(e => e.PatientProfiles)
                .Include(e => e.ChecklistItems)
                .FirstOrDefaultAsync(e => e.Id == id && e.CreatedByUserId == userId);

            if (examCase == null)
                return Forbid();

            return View(examCase);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] ExamCase examCase)
        {
            if (examCase == null)
                return BadRequest(new { success = false, error = "資料為空，無法新增" });

            // 自動補上建立者與時間
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                examCase.CreatedByUserId = userId;
            }
            else
            {

                examCase.CreatedByUserId = 0; // 或者省略
            }

            examCase.CreatedAt = DateTime.Now;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                return Json(new { success = false, error = "欄位驗證失敗", errors });
            }

            _context.ExamCases.Add(examCase);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                examCaseId = examCase.Id,
                message = "新增成功"
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAjax([FromBody] ExamCase updatedCase)
        {
            if (updatedCase == null || updatedCase.Id <= 0)
            {
                return BadRequest(new { success = false, message = "資料不正確" });
            }

            try
            {
                var dbCase = await _context.ExamCases.FindAsync(updatedCase.Id);
                if (dbCase == null)
                {
                    return NotFound(new { success = false, message = "找不到題組" });
                }

                // 更新欄位
                dbCase.Title = updatedCase.Title;
                dbCase.Description = updatedCase.Description;
                dbCase.PassScore = updatedCase.PassScore;
                dbCase.TimeLimit = updatedCase.TimeLimit;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "伺服器錯誤：" + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportFromDocument(IFormFile documentFile)
        {
            if (documentFile == null || documentFile.Length == 0)
            {
                TempData["Error"] = "請選擇要上傳的 Word 或 PDF 檔案。";
                return RedirectToAction(nameof(AddQ));
            }

            var extension = Path.GetExtension(documentFile.FileName);
            if (!string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "僅支援 .docx 與文字型 .pdf 格式。請將舊版 .doc 文件另存為 .docx 後再匯入。";
                return RedirectToAction(nameof(AddQ));
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                TempData["Error"] = "找不到使用者資訊。";
                return RedirectToAction(nameof(AddQ));
            }

            string docText;
            try
            {
                using var stream = documentFile.OpenReadStream();
                if (string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
                {
                    using var wordDoc = WordprocessingDocument.Open(stream, false);
                    var body = wordDoc.MainDocumentPart?.Document?.Body;
                    docText = body == null
                        ? string.Empty
                        : string.Join("\n", body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
                                                    .Select(p => p.InnerText));
                }
                else
                {
                    using var pdfDocument = PdfDocument.Open(stream);
                    docText = string.Join("\n", pdfDocument.GetPages().Select(page => page.Text));
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "無法讀取此檔案。請確認檔案未損壞或加密；PDF 必須是可選取文字的文字型 PDF。";
                return RedirectToAction(nameof(AddQ));
            }

            docText = DocumentParserHelper.NormalizeDocumentText(docText);

            var validation = ImportFormatValidationResult.Validate(docText);
            if (!validation.IsValid)
            {
                TempData["Error"] = "匯入取消：檔案格式與教案範本差異過大。\n" + string.Join("\n", validation.Errors.Select(error => $"• {error}"));
                return RedirectToAction(nameof(AddQ));
            }

            // --- 開始使用高容錯解析 ---

            // 2. 解析主檔資訊（與 Word/PDF 共用同一套容錯規則）
            var caseInfo = DocumentParserHelper.ParseExamCaseInfo(docText);

            var examCase = new ExamCase
            {
                Title = string.IsNullOrWhiteSpace(caseInfo.Title) ? "未命名教案" : caseInfo.Title,
                Description = caseInfo.Description ?? "",
                TimeLimit = caseInfo.TimeLimit ?? 8,
                PassScore = caseInfo.PassScore ?? 14,
                CreatedByUserId = int.Parse(userIdClaim.Value),
                CreatedAt = DateTime.Now
            };

            _context.ExamCases.Add(examCase);
            await _context.SaveChangesAsync();

            // 3. 呼叫神器：解析病人基本資料 (完全防呆)
            PatientProfile patient = DocumentParserHelper.ParsePatientProfile(docText);
            patient.ExamCaseId = examCase.Id;
            _context.PatientProfiles.Add(patient);
            await _context.SaveChangesAsync();

            // 4. 呼叫神器：解析評分項目，並自動轉成您需要的 JSON 格式
            List<ChecklistItem> checklists = DocumentParserHelper.ParseChecklists(docText);
            foreach (var item in checklists)
            {
                item.ExamCaseId = examCase.Id;

                // 處理關鍵字轉 JSON (轉成前端需要的 { phrase: "xxx" } 格式)
                var keywords = string.IsNullOrWhiteSpace(item.KeyPhrases)
                    ? new List<object>()
                    : item.KeyPhrases.Split(new[] { ',', '、' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(k => new { phrase = k.Trim() })
                                     .ToList<object>();

                item.KeyPhrases = JsonSerializer.Serialize(keywords);

                // 處理評分規則轉 JSON (從 Helper 抓到的字串拆解為 ViewModel)
                var scoringRules = new List<object>();
                var rulesText = item.ScoringRules ?? "";

                var perfectMatch = System.Text.RegularExpressions.Regex.Match(rulesText, @"完全做到[：:]\s*(.*?)(?=\n部分做到|\n沒有做到|$)");
                if (perfectMatch.Success && perfectMatch.Groups[1].Value != "X")
                    scoringRules.Add(new { Level = "完全做到", Score = 2, Description = perfectMatch.Groups[1].Value.Trim() });

                var partialMatch = System.Text.RegularExpressions.Regex.Match(rulesText, @"部分做到[：:]\s*(.*?)(?=\n沒有做到|$)");
                if (partialMatch.Success && partialMatch.Groups[1].Value != "X")
                    scoringRules.Add(new { Level = "部分做到", Score = 1, Description = partialMatch.Groups[1].Value.Trim() });

                var noneMatch = System.Text.RegularExpressions.Regex.Match(rulesText, @"沒有做到[：:]\s*(.*?)(?=$)");
                if (noneMatch.Success && noneMatch.Groups[1].Value != "X")
                    scoringRules.Add(new { Level = "沒有做到", Score = 0, Description = noneMatch.Groups[1].Value.Trim() });

                item.ScoringRules = JsonSerializer.Serialize(scoringRules);

                _context.ChecklistItems.Add(item);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "匯入成功！";
            return RedirectToAction(nameof(AddQ));
        }
    }
}
