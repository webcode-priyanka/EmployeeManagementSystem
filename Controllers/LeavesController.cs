using Microsoft.AspNetCore.Authorization;
using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,HR,Manager,Employee")]
    public class LeavesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeavesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // LEAVE LIST / HISTORY
        // =====================================================

        public async Task<IActionResult> Index(int page = 1)
        {
            // Records per page
            int pageSize = 10;

            // Page cannot be less than 1
            if (page < 1)
                page = 1;

            // Base query
            var query = _context.Leaves
                .Include(l => l.Employee)
                .AsQueryable();


            // =================================================
            // EMPLOYEE
            // Employee can see ONLY own leaves
            // =================================================

            if (User.IsInRole("Employee"))
            {
                var email = User.Identity?.Name;

                query = query.Where(l =>
                    l.Employee != null &&
                    l.Employee.Email == email);
            }


            // =================================================
            // MANAGER
            // Manager can see ONLY department/team leaves
            // =================================================

            else if (User.IsInRole("Manager"))
            {
                var email = User.Identity?.Name;

                var manager = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == email);

                if (manager == null)
                {
                    ViewBag.CurrentPage = 1;
                    ViewBag.TotalPages = 0;

                    return View(new List<Leave>());
                }

                query = query.Where(l =>
                    l.Employee != null &&
                    l.Employee.DepartmentId == manager.DepartmentId);
            }


            // =================================================
            // TOTAL RECORDS
            // =================================================

            var totalRecords = await query.CountAsync();


            // =================================================
            // TOTAL PAGES
            // =================================================

            var totalPages = (int)Math.Ceiling(
                totalRecords / (double)pageSize
            );


            // =================================================
            // PREVENT INVALID PAGE
            // =================================================

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }


            // =================================================
            // GET CURRENT PAGE RECORDS
            // =================================================

            var leaves = await query
                .OrderByDescending(l => l.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();


            // =================================================
            // PAGINATION INFORMATION
            // =================================================

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalRecords;


            return View(leaves);
        }


        // =====================================================
        // CREATE LEAVE - GET
        // ONLY EMPLOYEE CAN APPLY
        // =====================================================

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        // =====================================================
        // CREATE LEAVE - POST
        // ONLY EMPLOYEE CAN APPLY
        // =====================================================

        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Leave leave)
        {
            var email = User.Identity?.Name;


            // Find logged-in employee
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email == email);

            if (employee == null)
            {
                ModelState.AddModelError(
                    "",
                    "Employee profile was not found. Please contact administrator."
                );

                return View(leave);
            }


            // Date validation
            if (leave.FromDate > leave.ToDate)
            {
                ModelState.AddModelError(
                    "ToDate",
                    "To Date cannot be earlier than From Date."
                );
            }


            // Automatically assign EmployeeId
            leave.EmployeeId = employee.Id;


            // New leave is always Pending
            leave.Status = "Pending";


            // Save
            if (ModelState.IsValid)
            {
                _context.Leaves.Add(leave);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Leave application submitted successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(leave);
        }


        // =====================================================
        // EDIT LEAVE - GET
        // Employee can edit ONLY own Pending leave
        // =====================================================

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var email = User.Identity?.Name;

            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leave == null)
            {
                return NotFound();
            }

            if (leave.Employee?.Email != email ||
                leave.Status != "Pending")
            {
                return Forbid();
            }

            return View(leave);
        }


        // =====================================================
        // EDIT LEAVE - POST
        // =====================================================

        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Leave leave)
        {
            if (id != leave.Id)
            {
                return NotFound();
            }

            var email = User.Identity?.Name;

            var existingLeave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (existingLeave == null)
            {
                return NotFound();
            }


            // Only own Pending leave
            if (existingLeave.Employee?.Email != email ||
                existingLeave.Status != "Pending")
            {
                return Forbid();
            }


            // Date validation
            if (leave.FromDate > leave.ToDate)
            {
                ModelState.AddModelError(
                    "ToDate",
                    "To Date cannot be earlier than From Date."
                );
            }


            // Employee cannot change EmployeeId or Status
            existingLeave.LeaveType = leave.LeaveType;
            existingLeave.FromDate = leave.FromDate;
            existingLeave.ToDate = leave.ToDate;
            existingLeave.Reason = leave.Reason;
            existingLeave.Status = "Pending";


            if (ModelState.IsValid)
            {
                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Leave updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(leave);
        }


        // =====================================================
        // DELETE LEAVE - GET
        // Employee can delete ONLY own Pending leave
        // =====================================================

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var email = User.Identity?.Name;

            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leave == null)
            {
                return NotFound();
            }

            if (leave.Employee?.Email != email ||
                leave.Status != "Pending")
            {
                return Forbid();
            }

            return View(leave);
        }


        // =====================================================
        // DELETE LEAVE - POST
        // =====================================================

        [Authorize(Roles = "Employee")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var email = User.Identity?.Name;

            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leave == null)
            {
                return NotFound();
            }

            if (leave.Employee?.Email != email ||
                leave.Status != "Pending")
            {
                return Forbid();
            }

            _context.Leaves.Remove(leave);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Leave deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // APPROVE LEAVE
        // ADMIN / HR / MANAGER
        // =====================================================

        [Authorize(Roles = "Admin,HR,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leave == null)
            {
                return NotFound();
            }


            // Only Pending leave can be approved
            if (leave.Status != "Pending")
            {
                TempData["Error"] =
                    "Only pending leaves can be approved.";

                return RedirectToAction(nameof(Index));
            }


            // Manager can approve only team member leave
            if (User.IsInRole("Manager"))
            {
                var email = User.Identity?.Name;

                var manager = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == email);

                if (manager == null ||
                    leave.Employee == null ||
                    leave.Employee.DepartmentId != manager.DepartmentId)
                {
                    return Forbid();
                }
            }


            leave.Status = "Approved";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Leave approved successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // REJECT LEAVE
        // ADMIN / HR / MANAGER
        // =====================================================

        [Authorize(Roles = "Admin,HR,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leave == null)
            {
                return NotFound();
            }


            // Only Pending leave can be rejected
            if (leave.Status != "Pending")
            {
                TempData["Error"] =
                    "Only pending leaves can be rejected.";

                return RedirectToAction(nameof(Index));
            }


            // Manager can reject only team member leave
            if (User.IsInRole("Manager"))
            {
                var email = User.Identity?.Name;

                var manager = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == email);

                if (manager == null ||
                    leave.Employee == null ||
                    leave.Employee.DepartmentId != manager.DepartmentId)
                {
                    return Forbid();
                }
            }


            leave.Status = "Rejected";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Leave rejected successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}