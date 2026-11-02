using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EmployeeManagementSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // =====================================================
            // EMPLOYEE DASHBOARD
            // =====================================================

            if (User.IsInRole("Employee"))
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser != null)
                {
                    var employee = await _context.Employees
                        .Include(e => e.Department)
                        .FirstOrDefaultAsync(e => e.Email == currentUser.Email);

                    if (employee != null)
                    {
                        // Employee Profile
                        ViewBag.EmployeeName =
                            employee.FirstName + " " + employee.LastName;

                        ViewBag.Designation = employee.Designation;

                        ViewBag.DepartmentName =
                            employee.Department?.Name ?? "Not Assigned";

                        ViewBag.JoiningDate = employee.JoiningDate;


                        // My Attendance
                        ViewBag.MyAttendance =
                            await _context.Attendances
                                .CountAsync(a => a.EmployeeId == employee.Id);

                        ViewBag.MyPresentToday =
                            await _context.Attendances
                                .CountAsync(a =>
                                    a.EmployeeId == employee.Id &&
                                    a.Date == DateTime.Today &&
                                    a.Status == "Present");


                        // My Salary
                        ViewBag.MySalary =
                            await _context.Salaries
                                .Where(s => s.EmployeeId == employee.Id)
                                .OrderByDescending(s => s.Month)
                                .Select(s => (decimal?)s.NetSalary)
                                .FirstOrDefaultAsync() ?? 0;


                        // My Leaves
                        ViewBag.MyTotalLeaves =
                            await _context.Leaves
                                .CountAsync(l => l.EmployeeId == employee.Id);

                        ViewBag.MyPendingLeaves =
                            await _context.Leaves
                                .CountAsync(l =>
                                    l.EmployeeId == employee.Id &&
                                    l.Status == "Pending");

                        ViewBag.MyApprovedLeaves =
                            await _context.Leaves
                                .CountAsync(l =>
                                    l.EmployeeId == employee.Id &&
                                    l.Status == "Approved");
                    }

                }

                return View();
            }


            // =====================================================
            // ADMIN / HR / MANAGER DASHBOARD
            // =====================================================

            // Employees
            ViewBag.TotalEmployees =
                await _context.Employees.CountAsync();

            ViewBag.ActiveEmployees =
                await _context.Employees
                    .CountAsync(e => e.Status == "Active");

            ViewBag.InactiveEmployees =
                await _context.Employees
                    .CountAsync(e => e.Status != "Active");

            // Employee Status Chart
            ViewBag.EmployeeStatusLabels = new[] { "Active", "Inactive" };

            ViewBag.EmployeeStatusData = new[]
            {
               await _context.Employees.CountAsync(e => e.Status == "Active"),
               await _context.Employees.CountAsync(e => e.Status != "Active")
            };


            // Departments
            ViewBag.TotalDepartments =
                await _context.Departments.CountAsync();


            // =====================================================
            // ATTENDANCE
            // =====================================================

            ViewBag.PresentToday =
                await _context.Attendances
                    .CountAsync(a =>
                        a.Date == DateTime.Today &&
                        a.Status == "Present");

            ViewBag.AbsentToday =
                await _context.Attendances
                    .CountAsync(a =>
                        a.Date == DateTime.Today &&
                        a.Status == "Absent");

            ViewBag.TotalAttendance =
                await _context.Attendances.CountAsync();


            // =====================================================
            // SALARY
            // =====================================================

            ViewBag.TotalSalary =
                await _context.Salaries
                    .SumAsync(s => (decimal?)s.NetSalary) ?? 0;

            ViewBag.SalaryRecords =
                await _context.Salaries.CountAsync();


            // =====================================================
            // LEAVES
            // =====================================================

            ViewBag.TotalLeaves =
                await _context.Leaves.CountAsync();

            ViewBag.PendingLeaves =
                await _context.Leaves
                    .CountAsync(l => l.Status == "Pending");

            ViewBag.ApprovedLeaves =
                await _context.Leaves
                    .CountAsync(l => l.Status == "Approved");

            ViewBag.RejectedLeaves =
                await _context.Leaves
                    .CountAsync(l => l.Status == "Rejected");


            // =====================================================
            // RECENT EMPLOYEES
            // =====================================================

            ViewBag.RecentEmployees =
                await _context.Employees
                    .Include(e => e.Department)
                    .OrderByDescending(e => e.Id)
                    .Take(5)
                    .ToListAsync();


            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }


        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
        }
    }
}
