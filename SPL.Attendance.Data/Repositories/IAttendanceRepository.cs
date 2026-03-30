using SPL.Attendance.Data.Entities;

namespace SPL.Attendance.Data.Repositories
{
    /// <summary>
    /// Contract for all attendance-related database operations.
    /// The Business Layer depends on this interface, never on the concrete class.
    /// </summary>
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
    }
}
