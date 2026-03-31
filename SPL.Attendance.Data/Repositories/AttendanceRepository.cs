using Microsoft.EntityFrameworkCore;
using SPL.Attendance.Data.Context;
using SPL.Attendance.Data.Entities;

namespace SPL.Attendance.Data.Repositories
{
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

        public async Task AddLogAsync(AttendanceLog log)
        {
            await _context.AttendanceLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AttendanceLog>> GetLogsByEmployeeAsync(int employeeId)
        {
            return await _context.AttendanceLogs
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.LogDate)
                .ThenByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<AttendanceLog>> GetLogsByDateAsync(
            int employeeId, DateTime date)
        {
            return await _context.AttendanceLogs
                .Where(l => l.EmployeeId == employeeId
                         && l.LogDate == date.Date)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateLogAsync(AttendanceLog log)
        {
            _context.AttendanceLogs.Update(log);
            await _context.SaveChangesAsync();
        }

        public async Task<string> GetEmployeeNameAsync(int employeeId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId);
            return employee?.Name ?? "Unknown";
        }

        public async Task<AttendanceLog?> GetOpenLogAsync(int employeeId, DateTime date)
        {
            return await _context.AttendanceLogs
                .Where(l => l.EmployeeId == employeeId
                         && l.LogDate == date.Date
                         && l.CheckOutTime == null)
                .OrderByDescending(l => l.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Data.Entities.MonthlyAttendanceSummary?> GetMonthlyAsync(
    int employeeId, int month, int year)
        {
            return await _context.MonthlyAttendanceSummaries
                .FirstOrDefaultAsync(m =>
                    m.EmployeeId == employeeId &&
                    m.Month == month &&
                    m.Year == year);
        }

        public async Task UpsertMonthlyAsync(
            Data.Entities.MonthlyAttendanceSummary summary)
        {
            var existing = await GetMonthlyAsync(
                summary.EmployeeId, summary.Month, summary.Year);

            if (existing == null)
                await _context.MonthlyAttendanceSummaries.AddAsync(summary);
            else
            {
                existing.TotalDays = summary.TotalDays;
                existing.IsReset = false;
                existing.ResetAt = null;
                _context.MonthlyAttendanceSummaries.Update(existing);
            }

            await _context.SaveChangesAsync();
        }

        public async Task ResetMonthlyAsync(
            int employeeId, int month, int year, string managerName)
        {
            var summary = await GetMonthlyAsync(employeeId, month, year);
            if (summary == null) return;

            summary.TotalDays = 0;
            summary.IsReset = true;
            summary.ResetAt = DateTime.Now;
            summary.ResetByManager = managerName;

            _context.MonthlyAttendanceSummaries.Update(summary);
            await _context.SaveChangesAsync();
        }
    }
}
