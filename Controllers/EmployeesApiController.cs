using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Controllers
{
    [Route("api/employees")]
    [ApiController]
    public class EmployeesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmployeesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/employees
        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _context.Employees
                .ToListAsync();

            return Ok(employees);
        }

        // GET: api/employees/2
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound(new
                {
                    message = "Employee not found"
                });
            }

            return Ok(employee);
        }

        // POST: api/employees
        [HttpPost]
        public async Task<IActionResult> CreateEmployee(Employee employee)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEmployee),
                new { id = employee.Id },
                employee
            );
        }

        // PUT: api/employees/42
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, Employee employee)
        {
            if (id != employee.Id)
            {
                return BadRequest(new
                {
                    message = "ID mismatch"
                });
            }

            var existingEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (existingEmployee == null)
            {
                return NotFound(new
                {
                    message = "Employee not found"
                });
            }

            existingEmployee.FirstName = employee.FirstName;
            existingEmployee.LastName = employee.LastName;
            existingEmployee.Email = employee.Email;
            existingEmployee.Phone = employee.Phone;
            existingEmployee.DateOfBirth = employee.DateOfBirth;
            existingEmployee.JoiningDate = employee.JoiningDate;
            existingEmployee.Designation = employee.Designation;
            existingEmployee.Salary = employee.Salary;
            existingEmployee.DepartmentId = employee.DepartmentId;
            existingEmployee.Status = employee.Status;

            await _context.SaveChangesAsync();

            return Ok(existingEmployee);
        }

        // DELETE: api/employees/42
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound(new
                {
                    message = "Employee not found"
                });
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Employee deleted successfully"
            });
        }

    }
}