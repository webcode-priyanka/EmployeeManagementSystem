using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Department name is required")]
        [StringLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public ICollection<Employee>? Employees { get; set; }
    }
}
