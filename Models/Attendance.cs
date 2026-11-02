using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeManagementSystem.Models
{
    public class Attendance
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [DataType(DataType.Time)]
        public DateTime? CheckIn { get; set; }

        [DataType(DataType.Time)]
        public DateTime? CheckOut { get; set; }

        // Calculate total working hours
        [NotMapped]
        public string WorkingHours
        {
            get
            {
                if (CheckIn.HasValue && CheckOut.HasValue)
                {
                    var duration = CheckOut.Value - CheckIn.Value;

                    return $"{(int)duration.TotalHours}h {duration.Minutes}m";
                }

                return "-";
            }
        }

        [Required]
        public string Status { get; set; } = "Present";
    }
}