using SPL.Attendance.Data.Entities;

namespace SPL.Attendance.Data.Repositories
{
    /// <summary>
    /// Contract for all employee-related database operations.
    /// </summary>
    public interface IEmployeeRepository
    {
        Task<List<Employee>> GetAllAsync();
        Task<Employee?> GetByIdAsync(int id);
        Task<Employee?> GetByCodeAsync(string employeeCode);
        Task<Employee> AddAsync(Employee employee);
        Task<Employee> UpdateAsync(Employee employee);
        Task<bool> DeactivateAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> CodeExistsAsync(string employeeCode, int? excludeId = null);
        Task<Employee?> GetByEmailAsync(string email);
    }
}
