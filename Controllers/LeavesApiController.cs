using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Controllers
{
    [Route("api/leaves")]
    [ApiController]
    public class LeavesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeavesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/leaves
        [HttpGet]
        public async Task<IActionResult> GetLeaves()
        {
            var leaves = await _context.Leaves
                .Include(l => l.Employee)
                .ToListAsync();

            return Ok(leaves);
        }

        // GET: api/leaves/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLeave(int id)
        {
            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leave == null)
            {
                return NotFound(new
                {
                    message = "Leave not found"
                });
            }

            return Ok(leave);
        }

        // POST: api/leaves
        [HttpPost]
        public async Task<IActionResult> CreateLeave(Leave leave)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (leave.ToDate < leave.FromDate)
            {
                return BadRequest(new
                {
                    message = "To Date cannot be earlier than From Date"
                });
            }

            _context.Leaves.Add(leave);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetLeave),
                new { id = leave.Id },
                leave
            );
        }

        // PUT: api/leaves/1
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLeave(int id, Leave leave)
        {
            if (id != leave.Id)
            {
                return BadRequest(new
                {
                    message = "ID mismatch"
                });
            }

            var existingLeave = await _context.Leaves
                .FirstOrDefaultAsync(l => l.Id == id);

            if (existingLeave == null)
            {
                return NotFound(new
                {
                    message = "Leave not found"
                });
            }

            if (leave.ToDate < leave.FromDate)
            {
                return BadRequest(new
                {
                    message = "To Date cannot be earlier than From Date"
                });
            }

            existingLeave.EmployeeId = leave.EmployeeId;
            existingLeave.LeaveType = leave.LeaveType;
            existingLeave.FromDate = leave.FromDate;
            existingLeave.ToDate = leave.ToDate;
            existingLeave.Reason = leave.Reason;
            existingLeave.Status = leave.Status;

            await _context.SaveChangesAsync();

            return Ok(existingLeave);
        }

        // DELETE: api/leaves/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLeave(int id)
        {
            var leave = await _context.Leaves
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leave == null)
            {
                return NotFound(new
                {
                    message = "Leave not found"
                });
            }

            _context.Leaves.Remove(leave);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Leave deleted successfully"
            });
        }
    }
}
