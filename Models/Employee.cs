using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^[0-9]{10}$",
        ErrorMessage = "Phone number must contain exactly 10 digits")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Date of Birth is required")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Joining Date is required")]
        [DataType(DataType.Date)]
        public DateTime JoiningDate { get; set; }

        [Required(ErrorMessage = "Designation is required")]
        public string Designation { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Salary must be greater than or equal to 0")]
        [Precision(18, 2)]
        public decimal Salary { get; set; }

        public int? DepartmentId { get; set; }

        public Department? Department { get; set; }

        public string Status { get; set; } = "Active";
    }
}
