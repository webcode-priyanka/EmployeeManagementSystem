using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EmployeeManagementSystem.Models;

namespace EmployeeManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }

        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Leave> Leaves { get; set; }

        public DbSet<Salary> Salaries { get; set; }

        public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
    }
}