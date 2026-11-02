using Microsoft.AspNetCore.Authorization;
using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Controllers
{
    [Authorize]
    public class SalariesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalariesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // SALARY LIST
        // Admin/HR -> All salaries
        // Employee -> Only own salary
        // Manager -> No access
        // =====================================================

        [Authorize(Roles = "Admin,HR,Employee")]
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10;

            var query = _context.Salaries
                .Include(s => s.Employee)
                .AsQueryable();

            // Employee can see only own salary
            if (User.IsInRole("Employee"))
            {
                var email = User.Identity?.Name;

                query = query.Where(s =>
                    s.Employee != null &&
                    s.Employee.Email == email);
            }

            // Total records
            var totalRecords = await query.CountAsync();

            // Total pages
            var totalPages = (int)Math.Ceiling(
                totalRecords / (double)pageSize
            );

            // Prevent invalid page number
            if (page < 1)
            {
                page = 1;
            }

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // Get paginated salaries
            var salaries = await query
                .OrderByDescending(s => s.Month)
                .ThenByDescending(s => s.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalRecords;

            return View(salaries);
        }


        // =====================================================
        // CREATE SALARY - GET
        // Only Admin & HR
        // =====================================================

        [Authorize(Roles = "Admin,HR")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var employees = await _context.Employees
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();

            ViewBag.Employees = new SelectList(
                employees,
                "Id",
                "FirstName"
            );

            return View();
        }


        // =====================================================
        // CREATE SALARY - POST
        // Only Admin & HR
        // =====================================================

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Salary salary)
        {
            // Check employee exists
            var employeeExists = await _context.Employees
                .AnyAsync(e => e.Id == salary.EmployeeId);

            if (!employeeExists)
            {
                ModelState.AddModelError(
                    "EmployeeId",
                    "Please select a valid employee.");
            }

            // Prevent duplicate salary for same employee/month
            var duplicateSalary = await _context.Salaries
                .AnyAsync(s =>
                    s.EmployeeId == salary.EmployeeId &&
                    s.Month.Year == salary.Month.Year &&
                    s.Month.Month == salary.Month.Month);

            if (duplicateSalary)
            {
                ModelState.AddModelError(
                    "Month",
                    "Salary for this employee and month already exists.");
            }

            // Prevent negative net salary
            if (salary.BasicSalary + salary.Allowance - salary.Deduction < 0)
            {
                ModelState.AddModelError(
                    "Deduction",
                    "Deduction cannot be greater than Basic Salary + Allowance.");
            }

            if (ModelState.IsValid)
            {
                // NetSalary is calculated automatically
                _context.Salaries.Add(salary);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Salary added successfully.";

                return RedirectToAction(nameof(Index));
            }

            await LoadEmployees(salary.EmployeeId);

            return View(salary);
        }


        // =====================================================
        // EDIT SALARY - GET
        // Only Admin & HR
        // =====================================================

        [Authorize(Roles = "Admin,HR")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salary = await _context.Salaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salary == null)
            {
                return NotFound();
            }

            await LoadEmployees(salary.EmployeeId);

            return View(salary);
        }


        // =====================================================
        // EDIT SALARY - POST
        // Only Admin & HR
        // =====================================================

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Salary salary)
        {
            if (id != salary.Id)
            {
                return NotFound();
            }

            // Check employee exists
            var employeeExists = await _context.Employees
                .AnyAsync(e => e.Id == salary.EmployeeId);

            if (!employeeExists)
            {
                ModelState.AddModelError(
                    "EmployeeId",
                    "Please select a valid employee.");
            }

            // Prevent duplicate salary
            var duplicateSalary = await _context.Salaries
                .AnyAsync(s =>
                    s.Id != salary.Id &&
                    s.EmployeeId == salary.EmployeeId &&
                    s.Month.Year == salary.Month.Year &&
                    s.Month.Month == salary.Month.Month);

            if (duplicateSalary)
            {
                ModelState.AddModelError(
                    "Month",
                    "Salary for this employee and month already exists.");
            }

            // Prevent negative net salary
            if (salary.BasicSalary + salary.Allowance - salary.Deduction < 0)
            {
                ModelState.AddModelError(
                    "Deduction",
                    "Deduction cannot be greater than Basic Salary + Allowance.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Find existing salary
                    var existingSalary = await _context.Salaries
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (existingSalary == null)
                    {
                        return NotFound();
                    }

                    existingSalary.EmployeeId = salary.EmployeeId;
                    existingSalary.Month = salary.Month;
                    existingSalary.BasicSalary = salary.BasicSalary;
                    existingSalary.Allowance = salary.Allowance;
                    existingSalary.Deduction = salary.Deduction;

                    // NetSalary automatically recalculates
                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        "Salary updated successfully.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Salaries.Any(
                        s => s.Id == salary.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }
            }

            await LoadEmployees(salary.EmployeeId);

            return View(salary);
        }


        // =====================================================
        // DELETE SALARY - GET
        // Only Admin & HR
        // =====================================================

        [Authorize(Roles = "Admin,HR")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salary = await _context.Salaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salary == null)
            {
                return NotFound();
            }

            return View(salary);
        }


        // =====================================================
        // DELETE SALARY - POST
        // Only Admin & HR
        // =====================================================

        [Authorize(Roles = "Admin,HR")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var salary = await _context.Salaries
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salary == null)
            {
                return NotFound();
            }

            _context.Salaries.Remove(salary);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Salary deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // EMPLOYEE DROPDOWN
        // =====================================================

        private async Task LoadEmployees(int? selectedEmployeeId = null)
        {
            var employees = await _context.Employees
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();

            ViewBag.Employees = new SelectList(
                employees,
                "Id",
                "FirstName",
                selectedEmployeeId
            );
        }
    }
}