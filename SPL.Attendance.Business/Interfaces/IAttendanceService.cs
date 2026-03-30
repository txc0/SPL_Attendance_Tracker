using SPL.Attendance.Business.Models;

namespace SPL.Attendance.Business.Interfaces
{
    /// <summary>
    /// Contract for the Attendance Business Logic Layer.
    /// Controllers call this service; it never references HTTP or EF directly.
    /// </summary>
    public interface IAttendanceService
    {
        /// <summary>
        /// Records a check-in for today. Enforces: no duplicate check-in per day.
        /// </summary>
        /// <exception cref="InvalidOperationException">Employee already checked in today.</exception>
        /// <exception cref="KeyNotFoundException">Employee not found or inactive.</exception>
        Task CheckInAsync(int employeeId);

        /// <summary>
        /// Records a check-out for today, calculates work hours.
        /// Enforces: must have checked in; cannot check out twice.
        /// </summary>
        /// <exception cref="InvalidOperationException">No check-in found or already checked out.</exception>
        Task CheckOutAsync(int employeeId);

        /// <summary>Returns full attendance history for an employee, newest-first.</summary>
        Task<List<AttendanceRecordDto>> GetAttendanceHistoryAsync(int employeeId);

        /// <summary>Returns attendance for a specific date, or null if no record.</summary>
        Task<AttendanceRecordDto?> GetAttendanceByDateAsync(int employeeId, DateTime date);
    }
}
