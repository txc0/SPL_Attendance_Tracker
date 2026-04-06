using SPL.Attendance.Business.Models;

namespace SPL.Attendance.Business.Interfaces
{
    public interface IShowCauseService
    {

        /// <summary>Supervisor approves or rejects a show cause.</summary>
        Task ReviewAsync(int showCauseId, int supervisorId,
                         bool isApproved, string? reviewNote);

        Task<ShowCauseRequestDto> SubmitByEmailAsync(string email, string reason, string type);

        /// <summary>Get all pending show causes for a supervisor.</summary>
        Task<List<ShowCauseRequestDto>> GetPendingForSupervisorAsync(int supervisorId);

        /// <summary>Check if employee has a pending show cause.</summary>
        Task<ShowCauseRequestDto?> GetPendingForEmployeeAsync(int employeeId);
        Task<ShowCauseRequestDto> SubmitAsync(int employeeId, string reason, string type = "LOGIN");
    }
}