using SPL.Attendance.Business.Models;

namespace SPL.Attendance.Business.Interfaces
{
    public interface IShowCauseService
    {
        /// <summary>Employee submits a show cause reason.</summary>
        Task<ShowCauseRequestDto> SubmitAsync(int employeeId, string reason);

        /// <summary>Supervisor approves or rejects a show cause.</summary>
        Task ReviewAsync(int showCauseId, int supervisorId,
                         bool isApproved, string? reviewNote);

        /// <summary>Get all pending show causes for a supervisor.</summary>
        Task<List<ShowCauseRequestDto>> GetPendingForSupervisorAsync(
            int supervisorId);

        /// <summary>Check if employee has a pending show cause.</summary>
        Task<ShowCauseRequestDto?> GetPendingForEmployeeAsync(int employeeId);
    }
}