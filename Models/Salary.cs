using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeManagementSystem.Models
{
    public class Salary
    {
        public int Id { get; set; }

        // Employee relationship
        [Required]
        public int EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        // Salary Month
        [Required]
        [DataType(DataType.Date)]
        public DateTime Month { get; set; }

        // Basic Salary
        [Required]
        [Range(0, 999999999.99)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasicSalary { get; set; }

        // Allowance
        [Range(0, 999999999.99)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Allowance { get; set; }

        // Deduction
        [Range(0, 999999999.99)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Deduction { get; set; }

        // Net Salary = Basic Salary + Allowance - Deduction
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSalary
        {
            get
            {
                return BasicSalary + Allowance - Deduction;
            }
        }
    }
}