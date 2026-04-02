using BCrypt.Net;
using SPL.Attendance.Business.Interfaces;
using SPL.Attendance.Business.Models;
using SPL.Attendance.Data.Repositories;

namespace SPL.Attendance.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IEmployeeRepository _employeeRepo;

        public AuthService(IEmployeeRepository employeeRepo)
        {
            _employeeRepo = employeeRepo;
        }

        public async Task<LoginResultDto> LoginAsync(string email, string password)
        {
            // Find employee by email
            var employee = await _employeeRepo.GetByEmailAsync(email);

            if (employee == null || !employee.IsActive)
                throw new UnauthorizedAccessException(
                    "Invalid email or password.");

            // Check password is set
            if (string.IsNullOrEmpty(employee.PasswordHash))
                throw new UnauthorizedAccessException(
                    "Password not set for this account. Contact your manager.");

            // Verify password against hash
            bool valid = BCrypt.Net.BCrypt.Verify(password, employee.PasswordHash);

            if (!valid)
                throw new UnauthorizedAccessException(
                    "Invalid email or password.");

            return new LoginResultDto
            {
                EmployeeId = employee.Id,
                Name = employee.Name,
                Email = employee.Email ?? string.Empty,
                EmployeeCode = employee.EmployeeCode,
                IsSupervisor = employee.IsSupervisor,
                SupervisorId = employee.SupervisorId
            };
        }

        public string HashPassword(string plainPassword)
        {
            return BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);
        }

        public async Task SetPasswordAsync(int employeeId, string plainPassword)
        {
            var employee = await _employeeRepo.GetByIdAsync(employeeId);

            if (employee == null)
                throw new KeyNotFoundException(
                    $"Employee {employeeId} not found.");

            employee.PasswordHash = HashPassword(plainPassword);
            await _employeeRepo.UpdateAsync(employee);
        }
    }
}