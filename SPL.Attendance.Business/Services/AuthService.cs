using BCrypt.Net;
using SPL.Attendance.Business.Interfaces;
using SPL.Attendance.Business.Models;
using SPL.Attendance.Data.Entities;
using SPL.Attendance.Data.Repositories;

namespace SPL.Attendance.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IShowCauseRepository _showCauseRepo;
        private readonly ICompanyPolicyRepository _companyPolicyRepo;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(IEmployeeRepository employeeRepo,
                           IAttendanceRepository attendanceRepo,
                           IShowCauseRepository showCauseRepo,
                           ICompanyPolicyRepository companyPolicyRepo,
                           IUnitOfWork unitOfWork)
        {
            _employeeRepo = employeeRepo;
            _attendanceRepo = attendanceRepo;
            _showCauseRepo = showCauseRepo;
            _companyPolicyRepo = companyPolicyRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<LoginResultDto> LoginAsync(string email, string password)
        {
            // Find employee by email
            var employee = await _employeeRepo.GetByEmailAsync(email);

            var policy = await _companyPolicyRepo.GetActiveAsync();
            var now = DateTime.Now;
            var inPolicyWindow = policy != null
                && now.TimeOfDay >= policy.WorkStartTime
                && now.TimeOfDay <= policy.WorkEndTime;

            if (employee == null || !employee.IsActive)
                throw new UnauthorizedAccessException(
                    "Invalid email or password.");

            if (string.IsNullOrEmpty(employee.PasswordHash))
                throw new UnauthorizedAccessException(
                    "You have to set password manusally");

            bool valid = BCrypt.Net.BCrypt.Verify(password, employee.PasswordHash);
            if (!valid)
                throw new UnauthorizedAccessException(
                    "Invalid email or password.");

            var loginCount = await _attendanceRepo.GetLoginCountTodayAsync(employee.Id);

            // From 2nd login attempt onward, require admin approval for non-supervisors.
            if (!employee.IsSupervisor && loginCount >= 1)
            {
                var todayAttendance = await _attendanceRepo
                    .GetAttendanceAsync(employee.Id, DateTime.Today);

                var approved = await _showCauseRepo
                    .GetApprovedByEmployeeAsync(employee.Id, "LOGIN");

                var approvedForThisAttempt = approved != null
                    && approved.ReviewedAt.HasValue
                    && approved.ReviewedAt.Value.Date == DateTime.Today
                    && approved.ReviewedAt.Value > (todayAttendance?.CheckInTime ?? DateTime.MinValue);

                if (!approvedForThisAttempt)
                {
                    var pendingToday = await _showCauseRepo
                        .GetPendingByEmployeeAndDateAsync(employee.Id, DateTime.Today);

                    if (pendingToday == null)
                    {
                        var request = new ShowCauseRequest
                        {
                            EmployeeId = employee.Id,
                            SupervisorId = employee.SupervisorId ?? 1,
                            Reason = "Multiple login request",
                            Status = "Pending",
                            RequestedAt = DateTime.Now,
                            Type = "LOGIN"
                        };

                        await _showCauseRepo.AddAsync(request);
                    }

                    throw new UnauthorizedAccessException("Please wait for admin approval.");
                }
            }

            return new LoginResultDto
            {
                EmployeeId = employee.Id,
                Name = employee.Name,
                Email = employee.Email ?? string.Empty,
                EmployeeCode = employee.EmployeeCode,
                IsSupervisor = employee.IsSupervisor,
                SupervisorId = employee.SupervisorId
            };
        }

        public async Task RecordLoginAsync(int employeeId)
        {
            await _attendanceRepo.IncrementLoginCountAsync(employeeId);
            await _unitOfWork.SaveChangesAsync();

            var today = DateTime.Today;
            var attendance = await _attendanceRepo.GetAttendanceAsync(employeeId, today);
            var empName = await _attendanceRepo.GetEmployeeNameAsync(employeeId);

            if (attendance != null)
            {
                var log = new AttendanceLog
                {
                    AttendanceId = attendance.Id,
                    EmployeeId = employeeId,
                    EmployeeName = empName,
                    CheckInTime = DateTime.Now,
                    LogDate = today
                };
                await _attendanceRepo.AddLogAsync(log);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task RecordLogoutAsync(int employeeId)
        {
            var today = DateTime.Today;
            var openLog = await _attendanceRepo
                .GetOpenLogAsync(employeeId, today);

            if (openLog != null)
            {
                openLog.CheckOutTime = DateTime.Now;
                await _attendanceRepo.UpdateLogAsync(openLog);
            }

            var attendance = await _attendanceRepo.GetAttendanceAsync(employeeId, today);
            if (attendance == null)
            {
                await _unitOfWork.SaveChangesAsync();
                return;
            }

            attendance.LogoutCount += 1;
            attendance.CheckOutTime = DateTime.Now;
            attendance.IsCompleted = true;

            await _attendanceRepo.IncrementLogoutCountAsync(employeeId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> NeedsShowCauseForLoginAsync(int employeeId)
        {
            var loginCount = await _attendanceRepo
                .GetLoginCountTodayAsync(employeeId);
            return loginCount >= 1;
        }

        public async Task<bool> NeedsShowCauseForLogoutAsync(int employeeId)
        {
            var today = DateTime.Today;
            var attendance = await _attendanceRepo
                .GetAttendanceAsync(employeeId, today);
            if (attendance == null) return false;

            // Needs show cause if logout count >= 1
            return attendance.LogoutCount >= 1;
        }

        public string HashPassword(string plainPassword)
        {
            return BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);
        }

        public async Task SetPasswordAsync(int employeeId, string plainPassword)
        {
            var employee = await _employeeRepo.GetByIdAsync(employeeId)
                ?? throw new KeyNotFoundException(
                    $"Employee {employeeId} not found.");

            employee.PasswordHash = HashPassword(plainPassword);
            await _employeeRepo.UpdateAsync(employee);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}