using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AttendanceController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =========================================================
        // INDEX - ATTENDANCE REPORT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            int? employeeId,
            int? month,
            int? year,
            int page = 1)
        {
            const int pageSize = 10;

            var currentUser = await _userManager.GetUserAsync(User);

            bool isEmployee = User.IsInRole("Employee");

            int selectedMonth = month ?? DateTime.Today.Month;
            int selectedYear = year ?? DateTime.Today.Year;


            // -----------------------------------------------------
            // Validate Month
            // -----------------------------------------------------

            if (selectedMonth < 1 || selectedMonth > 12)
            {
                selectedMonth = DateTime.Today.Month;
            }


            // -----------------------------------------------------
            // Validate Year
            // -----------------------------------------------------

            if (selectedYear < 2020)
            {
                selectedYear = DateTime.Today.Year;
            }


            // =====================================================
            // EMPLOYEE LOGIN
            // =====================================================

            if (isEmployee)
            {
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e =>
                        e.Email == currentUser.Email);

                if (employee == null)
                {
                    return NotFound("Employee profile not found.");
                }

                // Employee can only see own attendance
                employeeId = employee.Id;

                ViewBag.SelectedEmployeeName =
                    $"{employee.FirstName} {employee.LastName}";

                ViewBag.SelectedEmployeeId = employee.Id;
            }


            // =====================================================
            // ADMIN / HR
            // =====================================================

            else
            {
                var employees = await _context.Employees
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .ToListAsync();

                ViewBag.Employees = employees;


                if (employeeId.HasValue)
                {
                    var selectedEmployee =
                        await _context.Employees
                        .FirstOrDefaultAsync(e =>
                            e.Id == employeeId.Value);

                    if (selectedEmployee == null)
                    {
                        return NotFound("Employee not found.");
                    }

                    ViewBag.SelectedEmployeeName =
                        $"{selectedEmployee.FirstName} " +
                        $"{selectedEmployee.LastName}";

                    ViewBag.SelectedEmployeeId =
                        selectedEmployee.Id;
                }
            }


            // =====================================================
            // NO EMPLOYEE SELECTED
            // =====================================================

            if (!employeeId.HasValue)
            {
                ViewBag.SelectedMonth = selectedMonth;
                ViewBag.SelectedYear = selectedYear;

                ViewBag.MonthName =
                    new DateTime(
                        selectedYear,
                        selectedMonth,
                        1)
                    .ToString("MMMM");

                ViewBag.PageSize = pageSize;
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 0;
                ViewBag.TotalRecords = 0;

                ViewBag.TotalDays = 0;
                ViewBag.PresentDays = 0;
                ViewBag.AbsentDays = 0;
                ViewBag.CheckedOutDays = 0;

                return View(new List<Attendance>());
            }


            // =====================================================
            // DATE RANGE
            // =====================================================

            DateTime startDate =
                new DateTime(
                    selectedYear,
                    selectedMonth,
                    1);

            DateTime endDate =
                startDate.AddMonths(1).AddDays(-1);


            // Don't count future dates as absent
            DateTime today = DateTime.Today;

            DateTime attendanceEndDate =
                endDate < today ? endDate : today;


            // =====================================================
            // GET REAL ATTENDANCE RECORDS
            // =====================================================

            var realAttendance = await _context.Attendances
                .Where(a =>
                    a.EmployeeId == employeeId.Value &&
                    a.Date >= startDate &&
                    a.Date <= endDate)
                .OrderBy(a => a.Date)
                .ToListAsync();


            // =====================================================
            // CREATE DAILY ATTENDANCE LIST
            // =====================================================

            var dailyAttendance =
                new List<Attendance>();


            // Only create dates up to today
            if (attendanceEndDate >= startDate)
            {
                for (
                    DateTime date = startDate;
                    date <= attendanceEndDate;
                    date = date.AddDays(1))
                {
                    var existingRecord =
                        realAttendance.FirstOrDefault(a =>
                            a.Date.Date == date.Date);


                    if (existingRecord != null)
                    {
                        dailyAttendance.Add(existingRecord);
                    }
                    else
                    {
                        // No attendance marked = Absent
                        dailyAttendance.Add(
                            new Attendance
                            {
                                Id = 0,
                                EmployeeId = employeeId.Value,
                                Date = date,
                                CheckIn = null,
                                CheckOut = null,
                                Status = "Absent"
                            });
                    }
                }
            }


            // =====================================================
            // STATISTICS
            // =====================================================

            int totalDays =
                dailyAttendance.Count;


            int presentDays =
                dailyAttendance.Count(a =>
                    a.Status == "Present");


            int absentDays =
                dailyAttendance.Count(a =>
                    a.Status == "Absent");


            int checkedOutDays =
                dailyAttendance.Count(a =>
                    a.CheckOut.HasValue);


            // =====================================================
            // PAGINATION
            // =====================================================

            // Latest date first
            dailyAttendance =
                dailyAttendance
                .OrderByDescending(a => a.Date)
                .ToList();


            int totalRecords =
                dailyAttendance.Count;


            int totalPages =
                (int)Math.Ceiling(
                    totalRecords /
                    (double)pageSize);


            if (page < 1)
            {
                page = 1;
            }


            if (totalPages > 0 &&
                page > totalPages)
            {
                page = totalPages;
            }


            var attendance =
                dailyAttendance
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();


            // =====================================================
            // VIEWBAG
            // =====================================================

            ViewBag.SelectedEmployeeId =
                employeeId;

            ViewBag.SelectedMonth =
                selectedMonth;

            ViewBag.SelectedYear =
                selectedYear;

            ViewBag.StartDate =
                startDate;

            ViewBag.EndDate =
                endDate;

            ViewBag.MonthName =
                startDate.ToString("MMMM");

            ViewBag.TotalDays =
                totalDays;

            ViewBag.PresentDays =
                presentDays;

            ViewBag.AbsentDays =
                absentDays;

            ViewBag.CheckedOutDays =
                checkedOutDays;

            ViewBag.CurrentPage =
                page;

            ViewBag.TotalPages =
                totalPages;

            ViewBag.TotalRecords =
                totalRecords;

            ViewBag.PageSize =
                pageSize;


            return View(attendance);
        }


        // =========================================================
        // CHECK IN
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> CheckIn()
        {
            var currentUser =
                await _userManager.GetUserAsync(User);


            if (currentUser == null)
            {
                return Unauthorized();
            }


            var employee =
                await _context.Employees
                .FirstOrDefaultAsync(e =>
                    e.Email == currentUser.Email);


            if (employee == null)
            {
                return NotFound(
                    "Employee profile not found.");
            }


            DateTime today =
                DateTime.Today;


            var attendance =
                await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == employee.Id &&
                    a.Date == today);


            // Already checked in
            if (attendance != null &&
                attendance.CheckIn.HasValue)
            {
                TempData["Error"] =
                    "You have already checked in today.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        employeeId = employee.Id,
                        month = today.Month,
                        year = today.Year
                    });
            }


            // Existing absent record
            if (attendance != null)
            {
                attendance.CheckIn =
                    DateTime.Now;

                attendance.Status =
                    "Present";
            }
            else
            {
                attendance =
                    new Attendance
                    {
                        EmployeeId =
                            employee.Id,

                        Date =
                            today,

                        CheckIn =
                            DateTime.Now,

                        Status =
                            "Present"
                    };

                _context.Attendances.Add(
                    attendance);
            }


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Attendance marked successfully. Check-in recorded.";


            return RedirectToAction(
                nameof(Index),
                new
                {
                    employeeId = employee.Id,
                    month = today.Month,
                    year = today.Year
                });
        }


        // =========================================================
        // CHECK OUT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> CheckOut()
        {
            var currentUser =
                await _userManager.GetUserAsync(User);


            if (currentUser == null)
            {
                return Unauthorized();
            }


            var employee =
                await _context.Employees
                .FirstOrDefaultAsync(e =>
                    e.Email == currentUser.Email);


            if (employee == null)
            {
                return NotFound(
                    "Employee profile not found.");
            }


            DateTime today =
                DateTime.Today;


            var attendance =
                await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == employee.Id &&
                    a.Date == today);


            // No attendance
            if (attendance == null)
            {
                TempData["Error"] =
                    "Please check in first.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        employeeId = employee.Id,
                        month = today.Month,
                        year = today.Year
                    });
            }


            // No Check-In
            if (!attendance.CheckIn.HasValue)
            {
                TempData["Error"] =
                    "Please check in first.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        employeeId = employee.Id,
                        month = today.Month,
                        year = today.Year
                    });
            }


            // Already checked out
            if (attendance.CheckOut.HasValue)
            {
                TempData["Error"] =
                    "You have already checked out today.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        employeeId = employee.Id,
                        month = today.Month,
                        year = today.Year
                    });
            }


            attendance.CheckOut =
                DateTime.Now;

            

            TempData["Success"] =
                "Check-out recorded successfully.";


            return RedirectToAction(
                nameof(Index),
                new
                {
                    employeeId = employee.Id,
                    month = today.Month,
                    year = today.Year
                });
        }
    }
}