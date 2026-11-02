using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Controllers
{
    [Route("api/salaries")]
    [ApiController]
    public class SalariesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SalariesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/salaries
        [HttpGet]
        public async Task<IActionResult> GetSalaries()
        {
            var salaries = await _context.Salaries
                .Include(s => s.Employee)
                .ToListAsync();

            return Ok(salaries);
        }

        // GET: api/salaries/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSalary(int id)
        {
            var salary = await _context.Salaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salary == null)
            {
                return NotFound(new
                {
                    message = "Salary record not found"
                });
            }

            return Ok(salary);
        }

        // POST: api/salaries
        [HttpPost]
        public async Task<IActionResult> CreateSalary(Salary salary)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == salary.EmployeeId);

            if (employee == null)
            {
                return BadRequest(new
                {
                    message = "Employee not found"
                });
            }

            if (salary.Deduction > salary.BasicSalary + salary.Allowance)
            {
                return BadRequest(new
                {
                    message = "Deduction cannot be greater than total salary"
                });
            }

            _context.Salaries.Add(salary);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetSalary),
                new { id = salary.Id },
                salary
            );
        }

        // PUT: api/salaries/1
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSalary(
            int id,
            Salary salary)
        {
            if (id != salary.Id)
            {
                return BadRequest(new
                {
                    message = "ID mismatch"
                });
            }

            var existingSalary = await _context.Salaries
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existingSalary == null)
            {
                return NotFound(new
                {
                    message = "Salary record not found"
                });
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == salary.EmployeeId);

            if (employee == null)
            {
                return BadRequest(new
                {
                    message = "Employee not found"
                });
            }

            if (salary.Deduction > salary.BasicSalary + salary.Allowance)
            {
                return BadRequest(new
                {
                    message = "Deduction cannot be greater than total salary"
                });
            }

            existingSalary.EmployeeId = salary.EmployeeId;
            existingSalary.Month = salary.Month;
            existingSalary.BasicSalary = salary.BasicSalary;
            existingSalary.Allowance = salary.Allowance;
            existingSalary.Deduction = salary.Deduction;

            await _context.SaveChangesAsync();

            return Ok(existingSalary);
        }

        // DELETE: api/salaries/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSalary(int id)
        {
            var salary = await _context.Salaries
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salary == null)
            {
                return NotFound(new
                {
                    message = "Salary record not found"
                });
            }

            _context.Salaries.Remove(salary);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Salary record deleted successfully"
            });
        }
    }
}
