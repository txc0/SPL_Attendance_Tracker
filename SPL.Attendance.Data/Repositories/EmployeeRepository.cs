using Microsoft.EntityFrameworkCore;
using SPL.Attendance.Data.Context;
using SPL.Attendance.Data.Entities;

namespace SPL.Attendance.Data.Repositories
{
    /// <summary>
    /// EF Core + MySQL implementation of IEmployeeRepository.
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly SPLAttendanceDbContext _context;

        public EmployeeRepository(SPLAttendanceDbContext context)
        {
            _context = context;
        }

        public async Task<List<Employee>> GetAllAsync()
        {
            return await _context.Employees
                .AsNoTracking()
                .Include(e => e.Supervisor)
                .Where(e => e.IsActive)
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.Supervisor)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Employee?> GetByCodeAsync(string employeeCode)
        {
            return await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode);
        }

        public async Task<Employee> AddAsync(Employee employee)
        {
            await _context.Employees.AddAsync(employee);
            return employee;
        }

        public Task<Employee> UpdateAsync(Employee employee)
        {
            _context.Employees.Update(employee);
            return Task.FromResult(employee);
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return false;

            employee.IsActive = false;
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Employees.AnyAsync(e => e.Id == id);
        }

        public async Task<bool> CodeExistsAsync(string employeeCode, int? excludeId = null)
        {
            return await _context.Employees.AnyAsync(e =>
                e.EmployeeCode == employeeCode &&
                (excludeId == null || e.Id != excludeId));
        }

        public async Task<Employee?> GetByEmailAsync(string email)
        {
            return await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.Email == email.ToLower().Trim() &&
                    e.IsActive);
        }
    }
}
