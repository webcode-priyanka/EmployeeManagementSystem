using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Controllers
{
    [Route("api/departments")]
    [ApiController]
    public class DepartmentsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/departments
        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await _context.Departments
                .ToListAsync();

            return Ok(departments);
        }

        // GET: api/departments/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepartment(int id)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return NotFound(new
                {
                    message = "Department not found"
                });
            }

            return Ok(department);
        }

        // POST: api/departments
        [HttpPost]
        public async Task<IActionResult> CreateDepartment(Department department)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetDepartment),
                new { id = department.Id },
                department
            );
        }

        // PUT: api/departments/1
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDepartment(
            int id,
            Department department)
        {
            if (id != department.Id)
            {
                return BadRequest(new
                {
                    message = "ID mismatch"
                });
            }

            var existingDepartment = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == id);

            if (existingDepartment == null)
            {
                return NotFound(new
                {
                    message = "Department not found"
                });
            }

            existingDepartment.Name = department.Name;
            existingDepartment.Description = department.Description;

            await _context.SaveChangesAsync();

            return Ok(existingDepartment);
        }

        // DELETE: api/departments/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return NotFound(new
                {
                    message = "Department not found"
                });
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Department deleted successfully"
            });
        }
    }
}
