using Microsoft.EntityFrameworkCore;
using SPL.Attendance.Data.Context;
using SPL.Attendance.Data.Entities;

namespace SPL.Attendance.Data.Repositories
{
    /// <summary>
    /// Concrete EF Core + MySQL implementation of IAttendanceRepository.
    /// </summary>
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly SPLAttendanceDbContext _context;

        public AttendanceRepository(SPLAttendanceDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<Entities.Attendance?> GetAttendanceAsync(int employeeId, DateTime date)
        {
            var dateOnly = date.Date;
            return await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId
                                       && a.AttendanceDate == dateOnly);
        }

        /// <inheritdoc />
        public async Task AddCheckInAsync(Entities.Attendance attendance)
        {
            await _context.Attendances.AddAsync(attendance);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task UpdateCheckOutAsync(Entities.Attendance attendance)
        {
            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<List<Entities.Attendance>> GetHistoryAsync(int employeeId)
        {
            return await _context.Attendances
                .Where(a => a.EmployeeId == employeeId)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Entities.Attendance?> GetByDateAsync(int employeeId, DateTime date)
        {
            return await GetAttendanceAsync(employeeId, date);
        }

        /// <inheritdoc />
        public async Task<bool> EmployeeExistsAsync(int employeeId)
        {
            return await _context.Employees
                .AnyAsync(e => e.Id == employeeId && e.IsActive);
        }
    }
}
