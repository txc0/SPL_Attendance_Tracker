using SPL.Attendance.Business.Interfaces;
using SPL.Attendance.Business.Models;
using SPL.Attendance.Data.Entities;
using SPL.Attendance.Data.Repositories;

namespace SPL.Attendance.Business.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IAttendanceRepository _repository;
        private readonly IShowCauseRepository _showCauseRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly ICompanyPolicyRepository _companyPolicyRepo;
        private readonly IUnitOfWork _unitOfWork;
        public AttendanceService(
            IAttendanceRepository repository,
            IShowCauseRepository showCauseRepo,
            IEmployeeRepository employeeRepo,
            ICompanyPolicyRepository companyPolicyRepo,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _showCauseRepo = showCauseRepo;
            _employeeRepo = employeeRepo;
            _companyPolicyRepo = companyPolicyRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task CheckInAsync(int employeeId)
        {
            var employee = await _employeeRepo.GetByIdAsync(employeeId);
            if (employee == null || !employee.IsActive)
                throw new KeyNotFoundException(
                    $"Employee with ID {employeeId} was not found or is inactive.");

            var policy = await _companyPolicyRepo.GetActiveAsync();
            var now = DateTime.Now;
            var inPolicyWindow = policy != null
                && now.TimeOfDay >= policy.WorkStartTime
                && now.TimeOfDay <= policy.WorkEndTime;

            var today = DateTime.Today;

            var todayAttendance = await _repository.GetAttendanceAsync(employeeId, today);

            // From 2nd sign-in onward, non-supervisors need one fresh approval per extra sign-in.
            if (!employee.IsSupervisor && todayAttendance != null && todayAttendance.LoginCount >= 1)
            {
                var approved = await _showCauseRepo.GetApprovedByEmployeeAsync(employeeId, "LOGIN");
                var approvedForThisAttempt = approved != null
                    && approved.ReviewedAt.HasValue
                    && approved.ReviewedAt.Value.Date == today
                    && approved.ReviewedAt.Value > (todayAttendance.CheckInTime ?? DateTime.MinValue);

                if (!approvedForThisAttempt)
                {
                    var pendingToday = await _showCauseRepo
                        .GetPendingByEmployeeAndDateAsync(employeeId, today);

                    if (pendingToday == null)
                    {
                        var request = new ShowCauseRequest
                        {
                            EmployeeId = employeeId,
                            SupervisorId = employee.SupervisorId ?? 1,
                            Reason = "Multiple login request",
                            Status = "Pending",
                            RequestedAt = now,
                            Type = "LOGIN"
                        };

                        await _showCauseRepo.AddAsync(request);
                        await _unitOfWork.SaveChangesAsync();

                        throw new InvalidOperationException(
                            "SHOW_CAUSE_REQUIRED: Please submit show cause and wait for admin approval.");
                    }

                    throw new InvalidOperationException(
                        "PENDING_APPROVAL: Your approval request is still pending.");
                }
            }

            if (todayAttendance == null)
            {
                todayAttendance = new Data.Entities.Attendance
                {
                    EmployeeId = employeeId,
                    AttendanceDate = today,
                    CheckInTime = now,
                    Status = "Present",
                    LoginCount = 1,
                    LogoutCount = 0
                };

                await _repository.AddCheckInAsync(todayAttendance);
            }
            else
            {
                todayAttendance.LoginCount += 1;

                // Keep the first check-in time of the day
                if (!todayAttendance.CheckInTime.HasValue)
                    todayAttendance.CheckInTime = now;

                await _repository.UpdateAttendanceAsync(todayAttendance);
            }

            var employeeName = await _repository.GetEmployeeNameAsync(employeeId);
            var log = new Data.Entities.AttendanceLog
            {
                AttendanceId = todayAttendance.Id,
                Attendance = todayAttendance,
                EmployeeId = employeeId,
                EmployeeName = employeeName,
                CheckInTime = now,
                CheckOutTime = null,
                LogDate = today
            };

            await _repository.AddLogAsync(log);
        }


        public async Task CheckOutAsync(int employeeId)
        {
            if (!await _repository.EmployeeExistsAsync(employeeId))
                throw new KeyNotFoundException(
                    $"Employee with ID {employeeId} was not found or is inactive.");

            var today = DateTime.Today;
            var now = DateTime.Now;

            var attendance = await _repository.GetAttendanceAsync(employeeId, today);

            if (attendance == null)
                throw new InvalidOperationException(
                    $"Employee {employeeId} has no check-in record for today. Please check in first.");

            var openLog = await _repository.GetOpenLogAsync(employeeId, today);

            if (openLog == null)
                throw new InvalidOperationException(
                    $"Employee {employeeId} has no open check-in to check out from. Please check in first.");

            var wasCompletedBeforeCheckout = attendance.IsCompleted;

            openLog.CheckOutTime = now;
            await _repository.UpdateLogAsync(openLog);

            attendance.CheckOutTime = now;
            attendance.LogoutCount += 1;

            var todayLogs = await _repository.GetLogsByDateAsync(employeeId, today);

            var firstCheckIn = todayLogs
                .Where(x => x.CheckInTime.HasValue)
                .Select(x => x.CheckInTime!.Value)
                .DefaultIfEmpty()
                .Min();

            var lastCheckOut = todayLogs
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

            await _repository.UpdateCheckOutAsync(attendance);

            if (!wasCompletedBeforeCheckout)
            {
                var summary = await _repository.GetMonthlyAsync(employeeId, now.Month, now.Year);

                if (summary == null)
                {
                    summary = new MonthlyAttendanceSummary
                    {
                        EmployeeId = employeeId,
                        Month = now.Month,
                        Year = now.Year,
                        TotalDays = 1
                    };
                }
                else
                {
                    summary.TotalDays += 1;
                }

                await _repository.UpsertMonthlyAsync(summary);
            }

            await _unitOfWork.SaveChangesAsync();
        }



        public async Task<List<AttendanceRecordDto>> GetAttendanceHistoryAsync(int employeeId)
        {
            var records = await _repository.GetHistoryAsync(employeeId);
            return records.Select(MapToDto).ToList();
        }

        public async Task<AttendanceRecordDto?> GetAttendanceByDateAsync(int employeeId, DateTime date)
        {
            var record = await _repository.GetByDateAsync(employeeId, date);
            return record == null ? null : MapToDto(record);
        }

        private static AttendanceRecordDto MapToDto(Data.Entities.Attendance a) => new()
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            EmployeeName = a.Employee?.Name ?? string.Empty,
            AttendanceDate = a.AttendanceDate,
            CheckInTime = a.CheckInTime,
            CheckOutTime = a.CheckOutTime,
            WorkHours = a.WorkHours,
            Status = a.Status
        };

        public Task<AttendanceRecordDto> GetEmployeeNameAsync(int employeeId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<AttendanceLogDto>> GetLogsAsync(int employeeId)
        {
            var logs = await _repository.GetLogsByEmployeeAsync(employeeId);
            return logs.Select(MapLogToDto).ToList();
        }

        public async Task<List<AttendanceLogDto>> GetLogsByDateAsync(
            int employeeId, DateTime date)
        {
            var logs = await _repository.GetLogsByDateAsync(employeeId, date);
            return logs.Select(MapLogToDto).ToList();
        }

        private static AttendanceLogDto MapLogToDto(Data.Entities.AttendanceLog l) => new()
        {
            Id = l.Id,
            EmployeeId = l.EmployeeId,
            EmployeeName = l.EmployeeName,
            CheckInTime = l.CheckInTime,
            CheckOutTime = l.CheckOutTime,
            LogDate = l.LogDate,
            CreatedAt = l.CreatedAt
        };

        public async Task<List<AttendanceRecordDto>> GetAllAttendanceAsync(string filter)
        {
            var today = DateTime.Today;
            DateTime from;
            DateTime to = today;

            switch (filter.ToLower())
            {
                case "week":
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                    from = today.AddDays(-diff);
                    break;
                case "month":
                    from = new DateTime(today.Year, today.Month, 1);
                    break;
                default:
                    from = today;
                    break;
            }

            var records = await _repository.GetAllByDateRangeAsync(from, to);

            return records.Select(MapToDto).ToList();
        }

        public async Task<MonthlyAttendanceSummaryDto> GetMonthlySummaryAsync(int employeeId, int month, int year)
        {
            if (!await _repository.EmployeeExistsAsync(employeeId))
                throw new KeyNotFoundException($"Employee with ID {employeeId} was not found or is inactive.");

            var summary = await _repository.GetMonthlyAsync(employeeId, month, year);

            if (summary == null)
            {
                return new MonthlyAttendanceSummaryDto
                {
                    EmployeeId = employeeId,
                    Month = month,
                    Year = year,
                    TotalDays = 0,
                    IsReset = false
                };
            }

            return new MonthlyAttendanceSummaryDto
            {
                EmployeeId = summary.EmployeeId,
                Month = summary.Month,
                Year = summary.Year,
                TotalDays = summary.TotalDays,
                IsReset = summary.IsReset,
                ResetAt = summary.ResetAt,
                ResetByManager = summary.ResetByManager
            };
        }

        public async Task ResetMonthlySummaryAsync(int employeeId, int month, int year, string managerName)
        {
            if (!await _repository.EmployeeExistsAsync(employeeId))
                throw new KeyNotFoundException($"Employee with ID {employeeId} was not found or is inactive.");

            if (string.IsNullOrWhiteSpace(managerName))
                throw new ArgumentException("Manager name is required.", nameof(managerName));

            await _repository.ResetMonthlyAsync(employeeId, month, year, managerName.Trim());
            await _unitOfWork.SaveChangesAsync();
        }
    }
}