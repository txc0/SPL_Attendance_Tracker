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
        }

        public Task UpdateCheckOutAsync(Entities.Attendance attendance)
        {
            _context.Attendances.Update(attendance);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<List<Entities.Attendance>> GetHistoryAsync(int employeeId)
        {
            return await _context.Attendances
                .AsNoTracking()
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
        }

        public Task UpdateLogAsync(AttendanceLog log)
        {
            _context.AttendanceLogs.Update(log);
            return Task.CompletedTask;
        }

        public async Task<string> GetEmployeeNameAsync(int employeeId)
        {
            return await _context.Employees
                .AsNoTracking()
                .Where(e => e.Id == employeeId)
                .Select(e => e.Name)
                .FirstOrDefaultAsync() ?? "Unknown";
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
            MonthlyAttendanceSummary summary)
        {
            var existing = await GetMonthlyAsync(summary.EmployeeId, summary.Month, summary.Year);

            if (existing == null)
            {
                await _context.MonthlyAttendanceSummaries.AddAsync(summary);
            }
            else
            {
                existing.TotalDays = summary.TotalDays;
                existing.IsReset = false;
                existing.ResetAt = null;
                existing.ResetByManager = null;
                _context.MonthlyAttendanceSummaries.Update(existing);
            }
        }

        public async Task ResetMonthlyAsync(
            int employeeId, int month, int year, string managerName)
        {
            var summary = await GetMonthlyAsync(employeeId, month, year);

            if (summary == null)
            {
                summary = new MonthlyAttendanceSummary
                {
                    EmployeeId = employeeId,
                    Month = month,
                    Year = year,
                    TotalDays = 0,
                    IsReset = true,
                    ResetAt = DateTime.Now,
                    ResetByManager = managerName
                };

                await _context.MonthlyAttendanceSummaries.AddAsync(summary);
            }
            else
            {
                summary.TotalDays = 0;
                summary.IsReset = true;
                summary.ResetAt = DateTime.Now;
                summary.ResetByManager = managerName;

                _context.MonthlyAttendanceSummaries.Update(summary);
            }
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

                if (!attendance.CheckInTime.HasValue)
                    attendance.CheckInTime = DateTime.Now;

                _context.Attendances.Update(attendance);
            }
        }

        public async Task IncrementLogoutCountAsync(int employeeId)
        {
            var today = DateTime.Today;
            var attendance = await GetAttendanceAsync(employeeId, today);

            if (attendance == null) return;

            attendance.LogoutCount += 1;

            var logs = await GetLogsByDateAsync(employeeId, today);

            var firstCheckIn = logs
                .Where(x => x.CheckInTime.HasValue)
                .Select(x => x.CheckInTime!.Value)
                .DefaultIfEmpty()
                .Min();

            var lastCheckOut = logs
                .Where(x => x.CheckOutTime.HasValue)
                .Select(x => x.CheckOutTime!.Value)
                .DefaultIfEmpty()
                .Max();

            if (firstCheckIn != default && lastCheckOut != default && lastCheckOut > firstCheckIn)
            {
                attendance.CheckInTime = firstCheckIn;
                attendance.CheckOutTime = lastCheckOut;
                attendance.WorkHours = Math.Round((decimal)(lastCheckOut - firstCheckIn).TotalHours, 2);
            }
            else
            {
                attendance.WorkHours = 0;
            }

            attendance.IsCompleted = true;

            _context.Attendances.Update(attendance);
        }

        public Task UpdateAttendanceAsync(Entities.Attendance attendance)
        {
            _context.Attendances.Update(attendance);
            return Task.CompletedTask;
        }

        public async Task<List<Entities.Attendance>> GetAllByDateRangeAsync(
    DateTime from, DateTime to)
        {
            return await _context.Attendances
                .AsNoTracking()
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
                .AsNoTracking()
                .Where(l => l.LogDate == date.Date && l.CheckOutTime == null)
                .Select(l => l.EmployeeId)
                .Distinct()
                .ToListAsync();
        }

        // Add this method to AttendanceRepository to fix CS0103
        private async Task<List<AttendanceLog>> GetLogsByDateAsync(int employeeId, DateTime date)
        {
            return await _context.AttendanceLogs
                .Where(l => l.EmployeeId == employeeId && l.LogDate == date.Date)
                .OrderBy(l => l.CheckInTime)
                .ToListAsync();
        }

        public async Task<List<AttendanceLog>> GetLogsByEmployeeAsync(int employeeId)
        {
            return await _context.AttendanceLogs
                .AsNoTracking()
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.LogDate)
                .ThenByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        Task<List<AttendanceLog>> IAttendanceRepository.GetLogsByDateAsync(int employeeId, DateTime date)
        {
            return GetLogsByDateAsync(employeeId, date);
        }
    }
}
