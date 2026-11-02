using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;

namespace EmployeeManagementSystem.Controllers
{
    [Authorize(Roles = "Employee")]
    public class MyProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MyProfileController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == user.Email);

            if (employee == null)
            {
                TempData["Error"] =
                    "Employee profile could not be found.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            // Employee ke documents
            var documents = await _context.EmployeeDocuments
                .Where(d => d.EmployeeId == employee.Id)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            ViewBag.Documents = documents;

            return View(employee);
        }
    }
}