using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EmployeeManagementSystem.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;

        public LoginModel(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [System.ComponentModel.DataAnnotations.Required(
                ErrorMessage = "Email address is required.")]
            [System.ComponentModel.DataAnnotations.EmailAddress(
                ErrorMessage = "Please enter a valid email address.")]
            public string Email { get; set; } = "";

            [System.ComponentModel.DataAnnotations.Required(
                ErrorMessage = "Password is required.")]
            [System.ComponentModel.DataAnnotations.DataType(
                System.ComponentModel.DataAnnotations.DataType.Password)]
            public string Password { get; set; } = "";

            [System.ComponentModel.DataAnnotations.Display(
                Name = "Remember me")]
            public bool RememberMe { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(
            string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your account is currently locked.");
            }
            else
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");
            }

            return Page();
        }
    }
}
