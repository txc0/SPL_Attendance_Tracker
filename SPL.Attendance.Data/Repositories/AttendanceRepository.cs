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

        public async Task<Entities.Attendance?> GetByDateAsync(int employeeId, DateTime date)
        {
            return await GetAttendanceAsync(employeeId, date);
        }

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

        public async Task<int> GetLoginCountTodayAsync(int employeeId)
        {
            var today = DateTime.Today;
            var attendance = await GetAttendanceAsync(employeeId, today);
            return attendance?.LoginCount ?? 0;
        }

        public async Task IncrementLoginCountAsync(int employeeId)
        {
            var today = DateTime.Today;
            var attendance = await GetAttendanceAsync(employeeId, today);

            if (attendance == null)
            {
                attendance = new Data.Entities.Attendance
                {
                    EmployeeId = employeeId,
                    AttendanceDate = today,
                    CheckInTime = DateTime.Now,
                    Status = "Present",
                    LoginCount = 1,
                    LogoutCount = 0
                };
                await _context.Attendances.AddAsync(attendance);
            }
            else
            {
                attendance.LoginCount += 1;
                attendance.CheckInTime = DateTime.Now;
                _context.Attendances.Update(attendance);
            }

            await _context.SaveChangesAsync();
        }

        public async Task IncrementLogoutCountAsync(int employeeId)
        {
            var today = DateTime.Today;
            var attendance = await GetAttendanceAsync(employeeId, today);

            if (attendance == null) return;

            attendance.LogoutCount += 1;
            attendance.CheckOutTime = DateTime.Now;

            // Calculate total work hours from all logs
            var logs = await GetLogsByDateAsync(employeeId, today);
            decimal totalHours = 0;
            foreach (var log in logs)
            {
                if (log.CheckInTime.HasValue && log.CheckOutTime.HasValue)
                    totalHours += (decimal)(log.CheckOutTime.Value
                                 - log.CheckInTime.Value).TotalHours;
            }
            attendance.WorkHours = Math.Round(totalHours, 2);
            attendance.IsCompleted = true;

            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAttendanceAsync(Entities.Attendance attendance)
        {
            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Entities.Attendance>> GetAllByDateRangeAsync(
    DateTime from, DateTime to)
        {
            return await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.AttendanceDate >= from.Date &&
                            a.AttendanceDate <= to.Date)
                .OrderByDescending(a => a.AttendanceDate)
                .ThenBy(a => a.Employee.Name)
                .ToListAsync();
        }

        public async Task<List<int>> GetEmployeesWithOpenLogsAsync(DateTime date)
        {
            return await _context.AttendanceLogs
                .Where(l => l.LogDate == date.Date && l.CheckOutTime == null)
                .Select(l => l.EmployeeId)
                .Distinct()
                .ToListAsync();
        }
    }
}
