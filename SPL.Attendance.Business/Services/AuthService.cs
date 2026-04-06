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

        public AuthService(IEmployeeRepository employeeRepo,
                           IAttendanceRepository attendanceRepo,
                           IShowCauseRepository showCauseRepo)
        {
            _employeeRepo = employeeRepo;
            _attendanceRepo = attendanceRepo;
            _showCauseRepo = showCauseRepo;
        }

        public async Task<LoginResultDto> LoginAsync(string email, string password)
        {
            // Find employee by email
            var employee = await _employeeRepo.GetByEmailAsync(email);

            if (employee == null || !employee.IsActive)
                throw new UnauthorizedAccessException(
                    "Invalid email or password.");

            if (string.IsNullOrEmpty(employee.PasswordHash))
                throw new UnauthorizedAccessException(
                    "Password not set. Contact your manager.");

            bool valid = BCrypt.Net.BCrypt.Verify(password, employee.PasswordHash);
            if (!valid)
                throw new UnauthorizedAccessException(
                    "Invalid email or password.");

            // Check if this is not the first login today
            var loginCount = await _attendanceRepo
                .GetLoginCountTodayAsync(employee.Id);

            if (loginCount >= 1 && !employee.IsSupervisor)
            {

                // Check if there is an approved show cause for login
                var approved = await _showCauseRepo
                    .GetApprovedByEmployeeAsync(employee.Id, "LOGIN");

                // Check if approved today
                bool approvedToday = approved != null &&
                    approved.ReviewedAt?.Date == DateTime.Today;

                if (!approvedToday)
                {
                    // Check if pending
                    var pending = await _showCauseRepo
                        .GetPendingByEmployeeAsync(employee.Id);

                    if (pending != null)
                        throw new UnauthorizedAccessException(
                            "SHOW_CAUSE_PENDING: Your request is waiting " +
                            "for supervisor approval.");

                    throw new UnauthorizedAccessException(
                        "SHOW_CAUSE_REQUIRED: You need supervisor approval " +
                        "to login again today.");
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

            // Add log entry
            var today = DateTime.Today;
            var attendance = await _attendanceRepo.GetAttendanceAsync(
                employeeId, today);
            var empName = await _attendanceRepo
                .GetEmployeeNameAsync(employeeId);

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

            await _attendanceRepo.IncrementLogoutCountAsync(employeeId);
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
        }
    }
}