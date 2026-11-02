using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeManagementSystem.Models
{
    public class Leave
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        [Display(Name = "Leave Type")]
        public string LeaveType { get; set; } = string.Empty;

        [Required]
        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [Required]
        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";
    }
}