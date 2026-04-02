using SPL.Attendance.Business.Models;

namespace SPL.Attendance.Business.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Validates email and password.
        /// Returns LoginResultDto on success.
        /// Throws UnauthorizedAccessException if credentials are wrong.
        /// </summary>
        Task<LoginResultDto> LoginAsync(string email, string password);

        /// <summary>Hashes a plain text password using BCrypt.</summary>
        string HashPassword(string plainPassword);

        /// <summary>Sets password for an employee (used for initial setup).</summary>
        Task SetPasswordAsync(int employeeId, string plainPassword);
    }
}