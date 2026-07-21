using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VirtualPatDiagnosis.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;



namespace VirtualPatDiagnosis.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserAccount userAccount, string ConfirmPassword)
        {
            if (!ModelState.IsValid)
                return View(userAccount);

            if (userAccount.Password != ConfirmPassword)
            {
                ModelState.AddModelError(nameof(ConfirmPassword), "密碼不一致");
                return View(userAccount);
            }

            bool exists = await _context.UserAccounts.AnyAsync(u => u.Username == userAccount.Username);
            if (exists)
            {
                ModelState.AddModelError(nameof(userAccount.Username), "帳號已存在");
                return View(userAccount);
            }

            _context.UserAccounts.Add(userAccount);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Username, string Password)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ModelState.AddModelError("", "帳號與密碼必填");
                return View();
            }

            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Username == Username && u.Password == Password);

            if (user == null)
            {
                ModelState.AddModelError("", "帳號或密碼錯誤");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), 
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("DisplayName", user.DisplayName ?? ""),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            if (user.Role == "teacher")
            {

                return RedirectToAction("AddQ", "ExamCase");

            }
            else if (user.Role == "student")
            {
                return RedirectToAction("CaseSelect", "Read");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }




        }

        // GET: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

    }
}
