using EmployeeManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailService _emailService;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        // =====================================================
        // LOGIN - GET
        // =====================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // =====================================================
        // LOGIN - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "email",
                    "Email address is required.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "password",
                    "Password is required.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");

                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "User is not allowed to login.");

                return View();
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "User account is locked.");

                return View();
            }

            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");

            return View();
        }

        // =====================================================
        // FORGOT PASSWORD - GET
        // =====================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // =====================================================
        // FORGOT PASSWORD - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "email",
                    "Email address is required.");

                return View();
            }

            if (!new EmailAddressAttribute().IsValid(email))
            {
                ModelState.AddModelError(
                    "email",
                    "Please enter a valid email address.");

                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    "email",
                    "No account found with this email address.");

                return View();
            }

            // =================================================
            // Generate Password Reset Token
            // =================================================

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(user);

            // =================================================
            // Generate Reset Password Link
            // =================================================

            var resetLink = Url.Action(
                "ResetPassword",
                "Auth",
                new
                {
                    email = user.Email,
                    token = token
                },
                Request.Scheme);

            // =================================================
            // Email HTML Body
            // =================================================

            string emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Password Reset</title>
</head>

<body style='font-family: Arial, sans-serif;
             background-color: #f5f7fb;
             padding: 30px;'>

    <div style='max-width:600px;
                margin:auto;
                background:white;
                padding:30px;
                border-radius:10px;
                box-shadow:0 2px 10px rgba(0,0,0,0.08);'>

        <h2 style='color:#2563eb;'>
            Password Reset Request
        </h2>

        <p>Hello,</p>

        <p>
            We received a request to reset your
            Employee Management System password.
        </p>

        <p>
            Click the button below to create a new password:
        </p>

        <p style='margin:30px 0;'>

            <a href='{resetLink}'
               style='background:#2563eb;
                      color:white;
                      padding:12px 22px;
                      text-decoration:none;
                      border-radius:6px;
                      display:inline-block;'>

                Reset Password

            </a>

        </p>

        <p>
            If you did not request a password reset,
            you can safely ignore this email.
        </p>

        <p>
            Regards,<br>
            <strong>Employee Management System</strong>
        </p>

    </div>

</body>
</html>";

            // =================================================
            // Testing
            // =================================================

            Console.WriteLine(
                "RESET LINK: " + resetLink);

            // =================================================
            // Send Email
            // =================================================

            await _emailService.SendEmailAsync(
                user.Email!,
                "Password Reset - Employee Management System",
                emailBody);

            TempData["Success"] =
                "Password reset link has been sent to your registered email address.";

            return RedirectToAction(nameof(Login));
        }

        // =====================================================
        // RESET PASSWORD - GET
        // =====================================================

        [HttpGet]
        public IActionResult ResetPassword(
            string email,
            string token)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] =
                    "Invalid or expired password reset link.";

                return RedirectToAction(nameof(Login));
            }

            // IMPORTANT:
            // Do NOT use Uri.UnescapeDataString() here.
            // ASP.NET Core handles the query-string decoding.

            ViewBag.Email = email;
            ViewBag.Token = token;

            return View();
        }

        // =====================================================
        // RESET PASSWORD - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            string email,
            string token,
            string password,
            string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "",
                    "Email address is required.");
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                ModelState.AddModelError(
                    "",
                    "Invalid password reset link.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "password",
                    "New password is required.");
            }

            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError(
                    "confirmPassword",
                    "Please confirm your new password.");
            }

            if (!string.IsNullOrWhiteSpace(password) &&
                password != confirmPassword)
            {
                ModelState.AddModelError(
                    "confirmPassword",
                    "Passwords do not match.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Email = email;
                ViewBag.Token = token;

                return View();
            }

            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid password reset request.");

                ViewBag.Email = email;
                ViewBag.Token = token;

                return View();
            }

            // =================================================
            // Reset Password
            // =================================================

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    password);

            if (result.Succeeded)
            {
                TempData["Success"] =
                    "Password reset successfully. You can now login.";

                return RedirectToAction(nameof(Login));
            }

            // =================================================
            // Display Identity Errors
            // =================================================

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }

            ViewBag.Email = email;
            ViewBag.Token = token;

            return View();
        }

        // =====================================================
        // REGISTRATION - GET
        // =====================================================

        [HttpGet]
        public IActionResult Registration()
        {
            return View();
        }

        // =====================================================
        // REGISTRATION - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registration(
            string email,
            string password,
            string confirmPassword)
        {
            // Email validation
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "email",
                    "Email address is required.");
            }
            else if (!new EmailAddressAttribute().IsValid(email))
            {
                ModelState.AddModelError(
                    "email",
                    "Please enter a valid email address.");
            }

            // Password validation
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "password",
                    "Password is required.");
            }
            else if (password.Length < 6)
            {
                ModelState.AddModelError(
                    "password",
                    "Password must be at least 6 characters.");
            }

            // Confirm password
            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError(
                    "confirmPassword",
                    "Please confirm your password.");
            }
            else if (password != confirmPassword)
            {
                ModelState.AddModelError(
                    "confirmPassword",
                    "Passwords do not match.");
            }

            // Stop if validation failed
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Check existing user
            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "email",
                    "An account with this email already exists.");

                return View();
            }

            // Create user
            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            // Save user
            var result =
                await _userManager.CreateAsync(
                    user,
                    password);

            if (result.Succeeded)
            {
                // Create Employee role if it doesn't exist
                if (!await _roleManager.RoleExistsAsync("Employee"))
                {
                    await _roleManager.CreateAsync(
                        new IdentityRole("Employee"));
                }

                // Assign Employee role
                await _userManager.AddToRoleAsync(
                    user,
                    "Employee");

                // Login user
                await _signInManager.SignInAsync(
                    user,
                    isPersistent: false);

                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            // Identity errors
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View();
        }

        // =====================================================
        // LOGOUT
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Login",
                "Auth");
        }
    }
}

