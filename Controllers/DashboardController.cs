using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;

namespace EmployeeManagementSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            // =====================================================
            // EMPLOYEE DASHBOARD
            // =====================================================

            if (User.IsInRole("Employee"))
            {
                var email = User.Identity?.Name;

                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Email == email);

                if (employee == null)
                {
                    return NotFound("Employee profile not found.");
                }

                ViewBag.EmployeeName =
                    $"{employee.FirstName} {employee.LastName}";

                ViewBag.Designation =
                    employee.Designation;

                ViewBag.DepartmentName =
                    employee.Department?.Name ?? "Not Assigned";

                ViewBag.JoiningDate =
                    employee.JoiningDate;


                // =================================================
                // MY ATTENDANCE
                // =================================================

                ViewBag.MyAttendance =
                    await _context.Attendances.CountAsync(a =>
                        a.EmployeeId == employee.Id);

                ViewBag.MyPresentToday =
                    await _context.Attendances
                        .Where(a =>
                            a.EmployeeId == employee.Id &&
                            a.Date.Date == today &&
                            a.Status == "Present")
                        .Select(a => a.EmployeeId)
                        .Distinct()
                        .CountAsync();


                // =================================================
                // MY LEAVES
                // =================================================

                ViewBag.MyTotalLeaves =
                    await _context.Leaves.CountAsync(l =>
                        l.EmployeeId == employee.Id);

                ViewBag.MyPendingLeaves =
                    await _context.Leaves.CountAsync(l =>
                        l.EmployeeId == employee.Id &&
                        l.Status == "Pending");

                ViewBag.MyApprovedLeaves =
                    await _context.Leaves.CountAsync(l =>
                        l.EmployeeId == employee.Id &&
                        l.Status == "Approved");


                // =================================================
                // MY SALARY
                // =================================================

                ViewBag.MySalary =
                    await _context.Salaries
                        .Where(s => s.EmployeeId == employee.Id)
                        .OrderByDescending(s => s.Month)
                        .Select(s =>
                            s.BasicSalary +
                            s.Allowance -
                            s.Deduction)
                        .FirstOrDefaultAsync();


                // =================================================
                // EMPLOYEE DASHBOARD MODEL
                // =================================================

                var employeeTotalAttendance =
                    await _context.Attendances.CountAsync(a =>
                        a.EmployeeId == employee.Id);

                var employeePresentCount =
                    await _context.Attendances
                        .Where(a =>
                            a.EmployeeId == employee.Id &&
                            a.Status == "Present")
                        .Select(a => a.Date.Date)
                        .Distinct()
                        .CountAsync();

                var employeeAbsentCount =
                    await _context.Attendances
                        .Where(a =>
                            a.EmployeeId == employee.Id &&
                            a.Status == "Absent")
                        .Select(a => a.Date.Date)
                        .Distinct()
                        .CountAsync();


                var employeeModel = new DashboardViewModel
                {
                    TotalEmployees = 0,
                    ActiveEmployees = 0,
                    InactiveEmployees = 0,
                    TotalDepartments = 0,

                    TotalAttendance =
                        employeeTotalAttendance,

                    PresentCount =
                        employeePresentCount,

                    AbsentCount =
                        employeeAbsentCount
                };


                // =================================================
                // EMPLOYEE ATTENDANCE CHART
                // =================================================

                var employeeAttendanceData =
                    await _context.Attendances
                        .Where(a =>
                            a.EmployeeId == employee.Id)
                        .GroupBy(a => a.Date.Date)
                        .Select(g => new
                        {
                            Date = g.Key,

                            Present = g.Count(a =>
                                a.Status == "Present"),

                            Absent = g.Count(a =>
                                a.Status == "Absent")
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();

                ViewBag.EmployeeAttendanceLabels =
                    employeeAttendanceData
                        .Select(x =>
                            x.Date.ToString("dd MMM"))
                        .ToArray();

                ViewBag.EmployeeAttendanceData =
                    employeeAttendanceData
                        .Select(x => x.Present)
                        .ToArray();


                return View(employeeModel);
            }


            // =====================================================
            // ADMIN / HR / MANAGER DASHBOARD
            // =====================================================

            var totalEmployees =
                await _context.Employees.CountAsync();

            var activeEmployees =
                await _context.Employees
                    .CountAsync(e => e.Status == "Active");

            var inactiveEmployees =
                await _context.Employees
                    .CountAsync(e => e.Status == "Inactive");

            var totalDepartments =
                await _context.Departments.CountAsync();


            // =====================================================
            // EMPLOYEE STATUS CHART
            // =====================================================

            ViewBag.EmployeeStatusLabels =
                new[]
                {
                    "Active",
                    "Inactive"
                };

            ViewBag.EmployeeStatusData =
                new[]
                {
                    activeEmployees,
                    inactiveEmployees
                };


            // =====================================================
            // DEPARTMENT-WISE EMPLOYEE CHART
            // =====================================================

            var departmentData =
                await _context.Departments
                    .Select(d => new
                    {
                        DepartmentName = d.Name,
                        EmployeeCount = d.Employees.Count()
                    })
                    .ToListAsync();

            ViewBag.DepartmentLabels =
                departmentData
                    .Select(d => d.DepartmentName)
                    .ToArray();

            ViewBag.DepartmentEmployeeData =
                departmentData
                    .Select(d => d.EmployeeCount)
                    .ToArray();


            // =====================================================
            // RECENT EMPLOYEES
            // =====================================================

            ViewBag.RecentEmployees =
                await _context.Employees
                    .Include(e => e.Department)
                    .OrderByDescending(e => e.Id)
                    .Take(5)
                    .ToListAsync();


            // =====================================================
            // ATTENDANCE SUMMARY
            // =====================================================

            // Employees marked Present today
            var presentEmployeeIdsToday =
                await _context.Attendances
                    .Where(a =>
                        a.Date.Date == today &&
                        a.Status == "Present")
                    .Select(a => a.EmployeeId)
                    .Distinct()
                    .ToListAsync();

            var presentToday =
                presentEmployeeIdsToday.Count;


            // Employees having any attendance record today
            var markedEmployeeIdsToday =
                await _context.Attendances
                    .Where(a =>
                        a.Date.Date == today)
                    .Select(a => a.EmployeeId)
                    .Distinct()
                    .ToListAsync();


            // Employees who are not marked present/absent today
            // are not counted as attendance records.
            var absentToday =
                activeEmployees - presentToday;


            if (absentToday < 0)
            {
                absentToday = 0;
            }


            // Total attendance records
            var totalAttendance =
                await _context.Attendances.CountAsync();


            ViewBag.PresentToday =
                presentToday;

            ViewBag.AbsentToday =
                absentToday;

            ViewBag.TotalAttendance =
                totalAttendance;


            // =====================================================
            // ATTENDANCE 7 DAYS CHART
            // =====================================================

            var last7Days =
                Enumerable
                    .Range(0, 7)
                    .Select(i => today.AddDays(-6 + i))
                    .ToList();


            var attendanceChartData =
                await _context.Attendances
                    .Where(a =>
                        a.Date.Date >= last7Days.First() &&
                        a.Date.Date <= last7Days.Last())
                    .GroupBy(a => a.Date.Date)
                    .Select(g => new
                    {
                        Date = g.Key,

                        Present =
                            g.Where(a =>
                                a.Status == "Present")
                             .Select(a => a.EmployeeId)
                             .Distinct()
                             .Count(),

                        Absent =
                            g.Where(a =>
                                a.Status == "Absent")
                             .Select(a => a.EmployeeId)
                             .Distinct()
                             .Count()
                    })
                    .ToListAsync();


            ViewBag.AttendanceChartLabels =
                last7Days
                    .Select(d =>
                        d.ToString("dd MMM"))
                    .ToArray();


            ViewBag.AttendancePresentData =
                last7Days
                    .Select(d =>
                        attendanceChartData
                            .Where(x => x.Date == d.Date)
                            .Select(x => x.Present)
                            .FirstOrDefault())
                    .ToArray();


            ViewBag.AttendanceAbsentData =
                last7Days
                    .Select(d =>
                        attendanceChartData
                            .Where(x => x.Date == d.Date)
                            .Select(x => x.Absent)
                            .FirstOrDefault())
                    .ToArray();


            // =====================================================
            // SALARY SUMMARY
            // =====================================================

            var totalSalary =
                await _context.Salaries
                    .SumAsync(s =>
                        s.BasicSalary +
                        s.Allowance -
                        s.Deduction);

            var salaryRecords =
                await _context.Salaries.CountAsync();

            ViewBag.TotalSalary =
                totalSalary;

            ViewBag.SalaryRecords =
                salaryRecords;


            // =====================================================
            // SALARY MONTHLY CHART
            // =====================================================

            var salaryChartData =
                await _context.Salaries
                    .GroupBy(s => new
                    {
                        s.Month.Year,
                        s.Month.Month
                    })
                    .Select(g => new
                    {
                        Year = g.Key.Year,

                        Month = g.Key.Month,

                        TotalSalary =
                            g.Sum(s =>
                                s.BasicSalary +
                                s.Allowance -
                                s.Deduction)
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();


            ViewBag.SalaryChartLabels =
                salaryChartData
                    .Select(x =>
                        new DateTime(
                            x.Year,
                            x.Month,
                            1)
                        .ToString("MMM yyyy"))
                    .ToArray();


            ViewBag.SalaryChartData =
                salaryChartData
                    .Select(x =>
                        x.TotalSalary)
                    .ToArray();


            // =====================================================
            // LEAVE SUMMARY
            // =====================================================

            var totalLeaves =
                await _context.Leaves.CountAsync();

            var pendingLeaves =
                await _context.Leaves
                    .CountAsync(l =>
                        l.Status == "Pending");

            var approvedLeaves =
                await _context.Leaves
                    .CountAsync(l =>
                        l.Status == "Approved");

            var rejectedLeaves =
                await _context.Leaves
                    .CountAsync(l =>
                        l.Status == "Rejected");


            ViewBag.TotalLeaves =
                totalLeaves;

            ViewBag.PendingLeaves =
                pendingLeaves;

            ViewBag.ApprovedLeaves =
                approvedLeaves;

            ViewBag.RejectedLeaves =
                rejectedLeaves;


            // =====================================================
            // LEAVE STATUS CHART
            // =====================================================

            ViewBag.LeaveStatusLabels =
                new[]
                {
                    "Pending",
                    "Approved",
                    "Rejected"
                };

            ViewBag.LeaveStatusData =
                new[]
                {
                    pendingLeaves,
                    approvedLeaves,
                    rejectedLeaves
                };


            // =====================================================
            // DASHBOARD MODEL
            // =====================================================

            var model = new DashboardViewModel
            {
                TotalEmployees =
                    totalEmployees,

                ActiveEmployees =
                    activeEmployees,

                InactiveEmployees =
                    inactiveEmployees,

                TotalDepartments =
                    totalDepartments,

                TotalAttendance =
                    totalAttendance,

                PresentCount =
                    presentToday,

                AbsentCount =
                    absentToday
            };


            // =====================================================
            // MANAGER DATA
            // =====================================================

            if (User.IsInRole("Manager"))
            {
                ViewBag.ManagerAttendanceLabels =
                    ViewBag.AttendanceChartLabels;

                ViewBag.ManagerAttendanceData =
                    ViewBag.AttendancePresentData;
            }


            // =====================================================
            // RETURN DASHBOARD
            // =====================================================

            return View(model);
        }
    }
}