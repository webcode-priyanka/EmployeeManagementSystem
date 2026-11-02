using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Controllers
{
    [Route("api/attendance")]
    [ApiController]
    public class AttendanceApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AttendanceApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/attendance
        [HttpGet]
        public async Task<IActionResult> GetAttendance()
        {
            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .ToListAsync();

            return Ok(attendance);
        }

        // GET: api/attendance/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAttendanceById(int id)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
            {
                return NotFound(new
                {
                    message = "Attendance record not found"
                });
            }

            return Ok(attendance);
        }

        // POST: api/attendance
        [HttpPost]
        public async Task<IActionResult> CreateAttendance(Attendance attendance)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == attendance.EmployeeId);

            if (employee == null)
            {
                return BadRequest(new
                {
                    message = "Employee not found"
                });
            }

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetAttendanceById),
                new { id = attendance.Id },
                attendance
            );
        }

        // PUT: api/attendance/1
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAttendance(
            int id,
            Attendance attendance)
        {
            if (id != attendance.Id)
            {
                return BadRequest(new
                {
                    message = "ID mismatch"
                });
            }

            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.Id == id);

            if (existingAttendance == null)
            {
                return NotFound(new
                {
                    message = "Attendance record not found"
                });
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == attendance.EmployeeId);

            if (employee == null)
            {
                return BadRequest(new
                {
                    message = "Employee not found"
                });
            }

            existingAttendance.EmployeeId = attendance.EmployeeId;
            existingAttendance.Date = attendance.Date;
            existingAttendance.CheckIn = attendance.CheckIn;
            existingAttendance.CheckOut = attendance.CheckOut;
            existingAttendance.Status = attendance.Status;

            await _context.SaveChangesAsync();

            return Ok(existingAttendance);
        }

        // DELETE: api/attendance/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
            {
                return NotFound(new
                {
                    message = "Attendance record not found"
                });
            }

            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Attendance record deleted successfully"
            });
        }
    }
}
