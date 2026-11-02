using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserRolesController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRolesController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // =====================================================
        // USER & ROLES LIST
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users
                .OrderBy(u => u.Email)
                .ToList();

            var userRoles = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userRoles[user.Id] =
                    roles.Any()
                        ? string.Join(", ", roles)
                        : "No Role";
            }

            ViewBag.UserRoles = userRoles;

            return View(users);
        }

        // =====================================================
        // MANAGE ROLE PAGE
        // =====================================================
        public async Task<IActionResult> Manage(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var currentRoles =
                await _userManager.GetRolesAsync(user);

            ViewBag.CurrentRole =
                currentRoles.FirstOrDefault() ?? "No Role";

            ViewBag.Roles = new[]
            {
                "Admin",
                "HR",
                "Manager",
                "Employee"
            };

            return View(user);
        }

        // =====================================================
        // UPDATE USER ROLE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(
            string id,
            string selectedRole)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(selectedRole))
            {
                TempData["Error"] =
                    "Please select a role.";

                return RedirectToAction(
                    nameof(Manage),
                    new { id });
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Prevent Admin from changing their own role
            var currentUserId =
                _userManager.GetUserId(User);

            if (user.Id == currentUserId)
            {
                TempData["Error"] =
                    "You cannot change your own role.";

                return RedirectToAction(nameof(Index));
            }

            // Make sure selected role exists
            if (!await _roleManager.RoleExistsAsync(selectedRole))
            {
                TempData["Error"] =
                    "Selected role does not exist.";

                return RedirectToAction(
                    nameof(Manage),
                    new { id });
            }

            // Get existing roles
            var existingRoles =
                await _userManager.GetRolesAsync(user);

            // Remove existing roles
            if (existingRoles.Any())
            {
                var removeResult =
                    await _userManager.RemoveFromRolesAsync(
                        user,
                        existingRoles);

                if (!removeResult.Succeeded)
                {
                    TempData["Error"] =
                        "Unable to remove existing role.";

                    return RedirectToAction(nameof(Index));
                }
            }

            // Add new role
            var addResult =
                await _userManager.AddToRoleAsync(
                    user,
                    selectedRole);

            if (!addResult.Succeeded)
            {
                TempData["Error"] =
                    "Unable to assign selected role.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                $"Role updated successfully. {user.Email} is now {selectedRole}.";

            return RedirectToAction(nameof(Index));
        }
    }
}
