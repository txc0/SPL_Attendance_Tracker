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
        /// <summary>
        /// Called on every login attempt.
        /// Checks if employee needs show cause before logging in.
        /// Returns LoginResultDto on success.
        /// Throws ShowCauseRequiredException if approval needed.
        /// </summary>

        /// <summary>Records check-in after successful login.</summary>
        Task RecordLoginAsync(int employeeId);

        /// <summary>Records check-out on logout.</summary>
        Task RecordLogoutAsync(int employeeId);

        /// <summary>Checks if employee needs show cause to login today.</summary>
        Task<bool> NeedsShowCauseForLoginAsync(int employeeId);

        /// <summary>Checks if employee needs show cause to logout today.</summary>
        Task<bool> NeedsShowCauseForLogoutAsync(int employeeId);
    }
}