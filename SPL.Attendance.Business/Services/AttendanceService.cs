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

        public AttendanceService(
            IAttendanceRepository repository,
            IShowCauseRepository showCauseRepo,
            IEmployeeRepository employeeRepo,
            ICompanyPolicyRepository companyPolicyRepo)
        {
            _repository = repository;
            _showCauseRepo = showCauseRepo;
            _employeeRepo = employeeRepo;
            _companyPolicyRepo = companyPolicyRepo;
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
                todayAttendance.CheckInTime = now;
                todayAttendance.IsCompleted = false;
                await _repository.UpdateAttendanceAsync(todayAttendance);
            }

            var employeeName = await _repository.GetEmployeeNameAsync(employeeId);
            var log = new Data.Entities.AttendanceLog
            {
                AttendanceId = todayAttendance.Id,
                EmployeeId = employeeId,
                EmployeeName = employeeName,
                CheckInTime = now,
                CheckOutTime = null,
                LogDate = today
            };

            await _repository.AddLogAsync(log);
        }


        //public async Task CheckInAsync(int employeeId)
        //{
        //    if (!await _repository.EmployeeExistsAsync(employeeId))
        //        throw new KeyNotFoundException(
        //            $"Employee with ID {employeeId} was not found or is inactive.");

        //    var today = DateTime.Today;
        //    var now = DateTime.Now;

        //    // Count how many log entries exist today
        //    var todayLogs = await _repository.GetLogsByDateAsync(employeeId, today);
        //    int totalLogsToday = todayLogs.Count;

        //    // If 2 or more complete sessions (2+ logs) ? require show cause
        //    // i.e. trying to check in a 3rd time
        //    if (totalLogsToday >= 2)
        //    {
        //        // Check if there is already an approved show cause for today
        //        var pending = await _showCauseRepo.GetPendingByEmployeeAsync(employeeId);

        //        if (pending != null)
        //            throw new InvalidOperationException(
        //                "SHOW_CAUSE_PENDING: Your show cause request is waiting " +
        //                "for supervisor approval.");

        //        throw new InvalidOperationException(
        //            "SHOW_CAUSE_REQUIRED: You have completed 2 sessions today. " +
        //            "Please submit a show cause reason to check in again.");
        //    }

        //    // Get or create daily attendance summary
        //    var attendance = await _repository.GetAttendanceAsync(employeeId, today);

        //    if (attendance == null)
        //    {
        //        attendance = new Data.Entities.Attendance
        //        {
        //            EmployeeId = employeeId,
        //            AttendanceDate = today,
        //            CheckInTime = now,
        //            Status = "Present"
        //        };
        //        await _repository.AddCheckInAsync(attendance);
        //    }

        //    // Write log entry
        //    var employeeName = await _repository.GetEmployeeNameAsync(employeeId);
        //    var log = new Data.Entities.AttendanceLog
        //    {
        //        AttendanceId = attendance.Id,
        //        EmployeeId = employeeId,
        //        EmployeeName = employeeName,
        //        CheckInTime = now,
        //        CheckOutTime = null,
        //        LogDate = today
        //    };

        //    await _repository.AddLogAsync(log);
        //}


        //public async Task CheckInAsync(int employeeId)
        //{
        //    if (!await _repository.EmployeeExistsAsync(employeeId))
        //        throw new KeyNotFoundException($"Employee with ID {employeeId} was not found or is inactive.");

        //    var today = DateTime.Today;
        //    var existing = await _repository.GetAttendanceAsync(employeeId, today);

        //    if (existing != null)
        //        throw new InvalidOperationException(
        //            $"Employee {employeeId} has already checked in today ({today:dd-MMM-yyyy}). " +
        //            "Duplicate check-in is not permitted.");

        //    var attendance = new Data.Entities.Attendance
        //    {
        //        EmployeeId    = employeeId,
        //        AttendanceDate = today,
        //        CheckInTime   = DateTime.Now,
        //        Status        = "Present"
        //    };

        //    await _repository.AddCheckInAsync(attendance);
        //    // Get employee name for the log
        //    var employee = await _repository.GetEmployeeNameAsync(employeeId);

        //    // Write check-in log entry
        //    var log = new Data.Entities.AttendanceLog
        //    {
        //        AttendanceId = attendance.Id,
        //        EmployeeId = employeeId,
        //        EmployeeName = employee,
        //        CheckInTime = attendance.CheckInTime,
        //        LogDate = today
        //    };
        //    await _repository.AddLogAsync(log);
        //}

        //public async Task CheckInAsync(int employeeId)
        //{
        //    if (!await _repository.EmployeeExistsAsync(employeeId))
        //        throw new KeyNotFoundException(
        //            $"Employee with ID {employeeId} was not found or is inactive.");

        //    var today = DateTime.Today;
        //    var now = DateTime.Now;

        //    // Get or create the daily attendance summary row
        //    var attendance = await _repository.GetAttendanceAsync(employeeId, today);

        //    if (attendance == null)
        //    {
        //        // First check-in of the day ? create the summary row
        //        attendance = new Data.Entities.Attendance
        //        {
        //            EmployeeId = employeeId,
        //            AttendanceDate = today,
        //            CheckInTime = now,
        //            Status = "Present"
        //        };
        //        await _repository.AddCheckInAsync(attendance);
        //    }
        //    // If attendance row already exists ? do NOT throw
        //    // Just allow a new log entry below

        //    // Always write a new log entry for every check-in
        //    var employeeName = await _repository.GetEmployeeNameAsync(employeeId);

        //    var log = new Data.Entities.AttendanceLog
        //    {
        //        AttendanceId = attendance.Id,
        //        EmployeeId = employeeId,
        //        EmployeeName = employeeName,
        //        CheckInTime = now,
        //        CheckOutTime = null,
        //        LogDate = today
        //    };

        //    await _repository.AddLogAsync(log);
        //}

        //public async Task CheckOutAsync(int employeeId)
        //{
        //    if (!await _repository.EmployeeExistsAsync(employeeId))
        //        throw new KeyNotFoundException($"Employee with ID {employeeId} was not found or is inactive.");

        //    var today = DateTime.Today;
        //    var attendance = await _repository.GetAttendanceAsync(employeeId, today);

        //    if (attendance == null)
        //        throw new InvalidOperationException(
        //            $"Employee {employeeId} has no check-in record for today ({today:dd-MMM-yyyy}). " +
        //            "Check-out requires a valid check-in.");

        //    if (attendance.CheckOutTime != null)
        //        throw new InvalidOperationException(
        //            $"Employee {employeeId} has already checked out today ({today:dd-MMM-yyyy}). " +
        //            "Duplicate check-out is not permitted.");

        //    attendance.CheckOutTime = DateTime.Now;

        //    attendance.WorkHours = (decimal)(attendance.CheckOutTime - attendance.CheckInTime)!
        //                           .Value.TotalHours;

        //    attendance.WorkHours = Math.Round(attendance.WorkHours.Value, 2);

        //    await _repository.UpdateCheckOutAsync(attendance);

        //}

        public async Task CheckOutAsync(int employeeId)
        {
            if (!await _repository.EmployeeExistsAsync(employeeId))
                throw new KeyNotFoundException(
                    $"Employee with ID {employeeId} was not found or is inactive.");

            var today = DateTime.Today;
            var now = DateTime.Now;

            var attendance = await _repository.GetAttendanceAsync(employeeId, today);

            // Must have checked in at least once today
            if (attendance == null)
                throw new InvalidOperationException(
                    $"Employee {employeeId} has no check-in record for today. " +
                    "Please check in first.");

            // Find the latest log entry that has no check-out yet
            var openLog = await _repository.GetOpenLogAsync(employeeId, today);

            if (openLog == null)
                throw new InvalidOperationException(
                    $"Employee {employeeId} has no open check-in to check out from. " +
                    "Please check in first.");

            // Fill check-out on the open log entry
            openLog.CheckOutTime = now;
            await _repository.UpdateLogAsync(openLog);

            // Update the summary row with latest check-out and total work hours
            attendance.CheckOutTime = now;

            // Calculate total work hours from all completed log entries today
            var todayLogs = await _repository.GetLogsByDateAsync(employeeId, today);
            decimal totalHours = 0;

            foreach (var log in todayLogs)
            {
                if (log.CheckInTime.HasValue && log.CheckOutTime.HasValue)
                {
                    totalHours += (int)(log.CheckOutTime.Value
                                 - log.CheckInTime.Value).TotalHours;
                }
            }

            attendance.WorkHours = Math.Round(totalHours, 2);
            attendance.IsCompleted = true;

            await _repository.UpdateCheckOutAsync(attendance);

            // Update monthly count
            var summary = await _repository.GetMonthlyAsync(
                employeeId, now.Month, now.Year);

            if (summary == null)
            {
                summary = new Data.Entities.MonthlyAttendanceSummary
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


    }
}