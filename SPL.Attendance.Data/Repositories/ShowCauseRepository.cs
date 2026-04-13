using Microsoft.EntityFrameworkCore;
using SPL.Attendance.Data.Context;
using SPL.Attendance.Data.Entities;

namespace SPL.Attendance.Data.Repositories
{
    public class ShowCauseRepository : IShowCauseRepository
    {
        private readonly SPLAttendanceDbContext _context;

        public ShowCauseRepository(SPLAttendanceDbContext context)
        {
            _context = context;
        }

        public async Task<ShowCauseRequest> AddAsync(ShowCauseRequest request)
        {
            await _context.ShowCauseRequests.AddAsync(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<ShowCauseRequest?> GetByIdAsync(int id)
        {
            return await _context.ShowCauseRequests
                .Include(s => s.Employee)
                .Include(s => s.Supervisor)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<ShowCauseRequest>> GetPendingBySupervisorAsync(
            int supervisorId)
        {
            return await _context.ShowCauseRequests
                .Include(s => s.Employee)
                .Where(s => s.SupervisorId == supervisorId &&
                            s.Status == "Pending")
                .OrderByDescending(s => s.RequestedAt)
                .ToListAsync();
        }

        public async Task<ShowCauseRequest?> GetPendingByEmployeeAsync(
            int employeeId)
        {
            return await _context.ShowCauseRequests
                .Where(s => s.EmployeeId == employeeId &&
                            s.Status == "Pending")
                .OrderByDescending(s => s.RequestedAt)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(ShowCauseRequest request)
        {
            _context.ShowCauseRequests.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task<ShowCauseRequest?> GetApprovedByEmployeeAsync(int employeeId, string type)
        {
            return await _context.ShowCauseRequests
                .Where(s => s.EmployeeId == employeeId &&
                            s.Type == type &&
                            s.Status == "Approved")
                .OrderByDescending(s => s.ReviewedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<ShowCauseRequest?> GetPendingByEmployeeAndDateAsync(int employeeId, DateTime date)
        {
            return await _context.ShowCauseRequests
                .Where(s => s.EmployeeId == employeeId
                         && s.Status == "Pending"
                         && s.RequestedAt.Date == date.Date)
                .FirstOrDefaultAsync();
        }
    }
}