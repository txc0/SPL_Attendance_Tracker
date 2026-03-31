using SPL.Attendance.Business.Models;

namespace SPL.Attendance.Business.Interfaces
{
 
    public interface IAttendanceService
    {
        Task CheckInAsync(int employeeId);

        Task CheckOutAsync(int employeeId);

        Task<List<AttendanceRecordDto>> GetAttendanceHistoryAsync(int employeeId);

        Task<AttendanceRecordDto?> GetAttendanceByDateAsync(int employeeId, DateTime date);
        Task<List<AttendanceLogDto>> GetLogsAsync(int employeeId);
        Task<List<AttendanceLogDto>> GetLogsByDateAsync(int employeeId, DateTime date);
    }
}
