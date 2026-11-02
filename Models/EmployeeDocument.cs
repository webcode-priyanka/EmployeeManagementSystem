
using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.Models
{
    public class EmployeeDocument
    {
        public int Id { get; set; }

        // Employee Relation
        [Required]
        public int EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        // Document Information
        [Required]
        [StringLength(100)]
        public string DocumentType { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public string FilePath { get; set; }

        // Upload Information
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Verification
        public string VerificationStatus { get; set; } = "Pending";

        public string? Remarks { get; set; }


    }
}