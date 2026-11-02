namespace EmployeeManagementSystem.Models
{
    public class DashboardViewModel
    {
        // Employee Summary
        public int TotalEmployees { get; set; }

        public int ActiveEmployees { get; set; }

        public int InactiveEmployees { get; set; }

        // Department Summary
        public int TotalDepartments { get; set; }

        // Attendance Summary
        public int TotalAttendance { get; set; }

        public int PresentCount { get; set; }

        public int AbsentCount { get; set; }
    }
}
