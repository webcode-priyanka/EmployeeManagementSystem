using EmployeeManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;

namespace EmployeeManagementSystem.Controllers
{
    [Authorize]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly EmailService _emailService;
        public EmployeesController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
             EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // =====================================================
        // EMPLOYEE LIST
        // =====================================================
        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> Index(
            string searchString,
            string status,
            int? departmentId,
            int page = 1)
        {
            int pageSize = 10;

            var employees = _context.Employees
                .Include(e => e.Department)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(e =>
                    e.FirstName.Contains(searchString) ||
                    e.LastName.Contains(searchString) ||
                    e.Email.Contains(searchString) ||
                    e.Id.ToString().Contains(searchString));
            }

            // Status Filter
            if (!string.IsNullOrEmpty(status))
            {
                employees = employees.Where(e =>
                    e.Status == status);
            }

            // Department Filter
            if (departmentId.HasValue)
            {
                employees = employees.Where(e =>
                    e.DepartmentId == departmentId.Value);
            }

            // Total Records
            var totalRecords = await employees.CountAsync();

            // Total Pages
            var totalPages = (int)Math.Ceiling(
                totalRecords / (double)pageSize);

            // Page validation
            if (page < 1)
            {
                page = 1;
            }

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // Pagination
            var employeeList = await employees
                .OrderByDescending(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Employee Documents
            var employeeIds = employeeList
                .Select(e => e.Id)
                .ToList();

            var employeeDocuments = await _context.EmployeeDocuments
                .Where(d => employeeIds.Contains(d.EmployeeId))
                .ToListAsync();

            ViewBag.EmployeeDocuments = employeeDocuments;

            // Department dropdown
            ViewBag.Departments =
                await _context.Departments.ToListAsync();

            // Filters
            ViewBag.SearchString = searchString;
            ViewBag.Status = status;
            ViewBag.DepartmentId = departmentId;

            // Pagination data
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalRecords;

            return View(employeeList);
        }

        // =====================================================
        // CREATE EMPLOYEE PAGE
        // =====================================================
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Departments =
                await _context.Departments.ToListAsync();

            return View();
        }

        // =====================================================
        // SAVE EMPLOYEE + CREATE LOGIN ACCOUNT
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Departments =
                    await _context.Departments.ToListAsync();

                return View(employee);
            }

            // Check Employee email
            var employeeExists = await _context.Employees
                .AnyAsync(e => e.Email == employee.Email);

            if (employeeExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "An employee with this email already exists.");

                ViewBag.Departments =
                    await _context.Departments.ToListAsync();

                return View(employee);
            }

            // Check Identity account
            var existingUser =
                await _userManager.FindByEmailAsync(employee.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "An account with this email already exists.");

                ViewBag.Departments =
                    await _context.Departments.ToListAsync();

                return View(employee);
            }

            // Generate temporary password
            string temporaryPassword =
                GenerateTemporaryPassword();

            // Create Identity User
            var user = new IdentityUser
            {
                UserName = employee.Email,
                Email = employee.Email,
                EmailConfirmed = true
            };

            var userResult =
                await _userManager.CreateAsync(
                    user,
                    temporaryPassword);

            if (!userResult.Succeeded)
            {
                foreach (var error in userResult.Errors)
                {
                    ModelState.AddModelError(
                        "Email",
                        error.Description);
                }

                ViewBag.Departments =
                    await _context.Departments.ToListAsync();

                return View(employee);
            }

            // Add Employee role
            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    "Employee");

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                ViewBag.Departments =
                    await _context.Departments.ToListAsync();

                return View(employee);
            }

            // Save Employee
            _context.Employees.Add(employee);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                await _userManager.DeleteAsync(user);
                throw;
            }

            // =====================================================
            // SEND WELCOME EMAIL
            // =====================================================

            try
            {
                var loginUrl = Url.Action(
                    "Login",
                    "Auth",
                    null,
                    Request.Scheme);

                var emailBody = $@"
        <div style='font-family: Arial, sans-serif; 
                    max-width: 600px; 
                    margin: auto; 
                    padding: 30px; 
                    background-color: #f5f7fb;'>

            <div style='background: white; 
                        padding: 30px; 
                        border-radius: 12px; 
                        box-shadow: 0 4px 15px rgba(0,0,0,0.08);'>

                <h2 style='color: #2563eb; margin-bottom: 20px;'>
                    Welcome to Employee Management System
                </h2>

                <p>
                    Hello <strong>{employee.FirstName} {employee.LastName}</strong>,
                </p>

                <p>
                    Your employee account has been created successfully.
                </p>

                <p>
                    You can now login to the Employee Management System 
                    using the credentials below:
                </p>

                <div style='background: #f1f5f9; 
                            padding: 20px; 
                            border-radius: 10px; 
                            margin: 20px 0;'>

                    <p style='margin: 8px 0;'>
                        <strong>Email:</strong> {employee.Email}
                    </p>

                    <p style='margin: 8px 0;'>
                        <strong>Temporary Password:</strong> {temporaryPassword}
                    </p>

                </div>

                <div style='text-align: center; margin: 25px 0;'>

                    <a href='{loginUrl}'
                       style='display: inline-block;
                              padding: 12px 25px;
                              background-color: #2563eb;
                              color: white;
                              text-decoration: none;
                              border-radius: 8px;
                              font-weight: bold;'>
                        Login to Your Account
                    </a>

                </div>

                <p style='color: #64748b; font-size: 14px;'>
                    Please change your password after your first login.
                </p>

                <p style='margin-top: 25px;'>
                    Regards,<br>
                    <strong>Employee Management System</strong>
                </p>

            </div>
        </div>";

                await _emailService.SendEmailAsync(
                    employee.Email,
                    "Welcome - Your Employee Account Has Been Created",
                    emailBody);
            }
            catch
            {
                TempData["Warning"] =
                    "Employee added successfully, but the welcome email could not be sent.";
            }

            // Success message
            TempData["Success"] =
                "Employee added successfully!";

            // Temporary login details
            TempData["LoginEmail"] =
                employee.Email;

            TempData["TemporaryPassword"] =
                temporaryPassword;

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // GENERATE TEMPORARY PASSWORD
        // =====================================================
        private string GenerateTemporaryPassword()
        {
            return $"Emp@{Guid.NewGuid().ToString("N")[..8]}1";
        }

        // =====================================================
        // EDIT EMPLOYEE PAGE
        // =====================================================
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee =
                await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            ViewBag.Departments =
                await _context.Departments.ToListAsync();

            return View(employee);
        }

        // =====================================================
        // UPDATE EMPLOYEE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(
            int id,
            Employee employee)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(employee);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Employee updated successfully!";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Departments =
                await _context.Departments.ToListAsync();

            return View(employee);
        }

        // =====================================================
        // EMPLOYEE DETAILS
        // =====================================================
        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            var documents = await _context.EmployeeDocuments
                .Where(d => d.EmployeeId == employee.Id)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            ViewBag.Documents = documents;

            return View(employee);
        }

        // =====================================================
        // DELETE CONFIRMATION
        // =====================================================
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee =
                await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }


        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            // Find Identity login account
            var user = await _userManager
                .FindByEmailAsync(employee.Email);

            // Delete employee documents first
            var documents = await _context.EmployeeDocuments
                .Where(d => d.EmployeeId == employee.Id)
                .ToListAsync();

            if (documents.Any())
            {
                _context.EmployeeDocuments.RemoveRange(documents);
            }

            // Delete attendance records
            var attendanceRecords = await _context.Attendances
                .Where(a => a.EmployeeId == employee.Id)
                .ToListAsync();

            if (attendanceRecords.Any())
            {
                _context.Attendances.RemoveRange(attendanceRecords);
            }

            // Delete employee
            _context.Employees.Remove(employee);

            await _context.SaveChangesAsync();

            // Delete Identity login account
            if (user != null)
            {
                var userResult = await _userManager.DeleteAsync(user);

                if (!userResult.Succeeded)
                {
                    TempData["Warning"] =
                        "Employee deleted, but login account could not be deleted.";

                    return RedirectToAction(nameof(Index));
                }
            }

            TempData["Success"] =
                "Employee and login account deleted successfully!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DEACTIVATE EMPLOYEE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var employee =
                await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            employee.Status = "Inactive";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Employee deactivated successfully!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // ACTIVATE EMPLOYEE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Activate(int id)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            employee.Status = "Active";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Employee activated successfully!";

            return RedirectToAction(nameof(Index));
        }
    }
}