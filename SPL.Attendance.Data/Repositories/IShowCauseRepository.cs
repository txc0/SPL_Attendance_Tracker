using SPL.Attendance.Data.Entities;

namespace SPL.Attendance.Data.Repositories
{
    public interface IShowCauseRepository
    {
        Task<ShowCauseRequest> AddAsync(ShowCauseRequest request);
        Task<ShowCauseRequest?> GetByIdAsync(int id);
        Task<List<ShowCauseRequest>> GetPendingBySupervisorAsync(int supervisorId);
        Task<ShowCauseRequest?> GetPendingByEmployeeAsync(int employeeId);
        Task UpdateAsync(ShowCauseRequest request);
        Task<ShowCauseRequest?> GetApprovedByEmployeeAsync(int employeeId, string type);
        Task<ShowCauseRequest?> GetPendingByEmployeeAndDateAsync(int employeeId, DateTime date);
    }
}