using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VirtualPatDiagnosis.Models;
namespace VirtualPatDiagnosis.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SaveAjax([FromBody] PatientProfile p)
        {
            if (string.IsNullOrWhiteSpace(p.Name))
                return BadRequest(new { success = false, message = "姓名不可空白" });

            if (p.Id == 0)
            {
                _context.PatientProfiles.Add(p);
            }
            else
            {
                var existing = await _context.PatientProfiles.FindAsync(p.Id);
                if (existing == null)
                    return NotFound(new { success = false, message = "找不到資料" });

                // ✅ 更新欄位
                existing.Name = p.Name;
                existing.Age = p.Age;
                existing.Complaint = p.Complaint;
                existing.CurrentHistory = p.CurrentHistory;
                existing.PastHistory = p.PastHistory;
                existing.FamilyHistory = p.FamilyHistory;
                existing.DrugHistory = p.DrugHistory;
                existing.RestrictionRules = p.RestrictionRules;
                existing.QA = p.QA;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var target = await _context.PatientProfiles.FindAsync(id);
            if (target == null)
                return NotFound(new { success = false, message = "找不到資料" });

            _context.PatientProfiles.Remove(target);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        

        public IActionResult Index()
        {
            return View();
        }
    }
}
