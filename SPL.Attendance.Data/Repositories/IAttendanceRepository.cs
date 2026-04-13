using SPL.Attendance.Data.Entities;

namespace SPL.Attendance.Data.Repositories
{
    public interface IAttendanceRepository
    {
        /// <summary>Retrieves the attendance record for a given employee on a given date, or null.</summary>
        Task<Entities.Attendance?> GetAttendanceAsync(int employeeId, DateTime date);

        /// <summary>Persists a new check-in record.</summary>
        Task AddCheckInAsync(Entities.Attendance attendance);

        /// <summary>Saves an updated attendance record (check-out + work hours).</summary>
        Task UpdateCheckOutAsync(Entities.Attendance attendance);

        /// <summary>Returns all attendance records for an employee, newest-first.</summary>
        Task<List<Entities.Attendance>> GetHistoryAsync(int employeeId);

        /// <summary>Returns the attendance record for a specific employee and date.</summary>
        Task<Entities.Attendance?> GetByDateAsync(int employeeId, DateTime date);

        /// <summary>Returns true if the employee exists and is active.</summary>
        Task<bool> EmployeeExistsAsync(int employeeId);

        Task AddLogAsync(AttendanceLog log);

        Task<List<AttendanceLog>> GetLogsByEmployeeAsync(int employeeId);

        Task<List<AttendanceLog>> GetLogsByDateAsync(int employeeId, DateTime date);

        Task UpdateLogAsync(AttendanceLog log);
        Task<string> GetEmployeeNameAsync(int employeeId);

        Task<AttendanceLog?> GetOpenLogAsync(int employeeId, DateTime date);
        Task<MonthlyAttendanceSummary?> GetMonthlyAsync(int employeeId, int month, int year);

        Task UpsertMonthlyAsync(Data.Entities.MonthlyAttendanceSummary summary);

        Task ResetMonthlyAsync(int employeeId, int month, int year, string managerName);

        Task<int> GetLoginCountTodayAsync(int employeeId);
        Task IncrementLoginCountAsync(int employeeId);
        Task IncrementLogoutCountAsync(int employeeId);

        Task UpdateAttendanceAsync(Entities.Attendance attendance);

        Task<List<Entities.Attendance>> GetAllByDateRangeAsync(DateTime from, DateTime to);

    }
}