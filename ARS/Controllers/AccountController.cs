using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ARS.Data;
using ARS.Models;
using ARS.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace ARS.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            if (IsUserLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "An account with this email already exists");
                return View(model);
            }

            // Create new user
            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = HashPassword(model.Password),
                Phone = model.Phone,
                Gender = model.Gender,
                Age = model.Age,
                Address = model.Address,
                Role = "Customer",
                SkyMiles = 0
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Log the user in
            SetUserSession(user);

            TempData["SuccessMessage"] = "Registration successful! Welcome to ARS.";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Login
        public IActionResult Login(string? returnUrl = null)
        {
            if (IsUserLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !VerifyPassword(model.Password, user.Password))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password");
                return View(model);
            }

            // Log the user in
            SetUserSession(user);

            TempData["SuccessMessage"] = $"Welcome back, {user.FirstName}!";

            // Redirect to return URL or home
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Profile
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", new { returnUrl = "/Account/Profile" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                UserID = user.UserID,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Gender = user.Gender,
                Age = user.Age,
                SkyMiles = user.SkyMiles
            };

            return View(model);
        }

        // POST: Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Check if email is being changed and if it's already taken
            if (user.Email != model.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.UserID != userId))
                {
                    ModelState.AddModelError("Email", "This email is already in use");
                    return View(model);
                }
            }

            // Update user information
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.Address = model.Address;
            user.Gender = model.Gender;
            user.Age = model.Age;

            await _context.SaveChangesAsync();

            // Update session
            SetUserSession(user);

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // GET: Account/ChangePassword
        public IActionResult ChangePassword()
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Verify current password
            if (!VerifyPassword(model.CurrentPassword, user.Password))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                return View(model);
            }

            // Update password
            user.Password = HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction("Profile");
        }

        // Helper methods
        private void SetUserSession(User user)
        {
            HttpContext.Session.SetInt32("UserId", user.UserID);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
            HttpContext.Session.SetString("UserRole", user.Role);
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        private bool IsUserLoggedIn()
        {
            return HttpContext.Session.GetInt32("UserId") != null;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            var hashedInput = HashPassword(password);
            return hashedInput == hashedPassword;
        }
    }
}
