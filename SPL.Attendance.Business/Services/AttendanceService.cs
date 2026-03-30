using SPL.Attendance.Business.Interfaces;
using SPL.Attendance.Business.Models;
using SPL.Attendance.Data.Entities;
using SPL.Attendance.Data.Repositories;

namespace SPL.Attendance.Business.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IAttendanceRepository _repository;

        public AttendanceService(IAttendanceRepository repository)
        {
            _repository = repository;
        }

        public async Task CheckInAsync(int employeeId)
        {
            if (!await _repository.EmployeeExistsAsync(employeeId))
                throw new KeyNotFoundException($"Employee with ID {employeeId} was not found or is inactive.");

            var today = DateTime.Today;
            var existing = await _repository.GetAttendanceAsync(employeeId, today);

            if (existing != null)
                throw new InvalidOperationException(
                    $"Employee {employeeId} has already checked in today ({today:dd-MMM-yyyy}). " +
                    "Duplicate check-in is not permitted.");

            var attendance = new Data.Entities.Attendance
            {
                EmployeeId    = employeeId,
                AttendanceDate = today,
                CheckInTime   = DateTime.Now,
                Status        = "Present"
            };

            await _repository.AddCheckInAsync(attendance);
        }

        public async Task CheckOutAsync(int employeeId)
        {
            if (!await _repository.EmployeeExistsAsync(employeeId))
                throw new KeyNotFoundException($"Employee with ID {employeeId} was not found or is inactive.");

            var today = DateTime.Today;
            var attendance = await _repository.GetAttendanceAsync(employeeId, today);

            if (attendance == null)
                throw new InvalidOperationException(
                    $"Employee {employeeId} has no check-in record for today ({today:dd-MMM-yyyy}). " +
                    "Check-out requires a valid check-in.");

            if (attendance.CheckOutTime != null)
                throw new InvalidOperationException(
                    $"Employee {employeeId} has already checked out today ({today:dd-MMM-yyyy}). " +
                    "Duplicate check-out is not permitted.");

            attendance.CheckOutTime = DateTime.Now;

            attendance.WorkHours = (decimal)(attendance.CheckOutTime - attendance.CheckInTime)!
                                   .Value.TotalHours;

            attendance.WorkHours = Math.Round(attendance.WorkHours.Value, 2);

            await _repository.UpdateCheckOutAsync(attendance);
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
            Id             = a.Id,
            EmployeeId     = a.EmployeeId,
            AttendanceDate = a.AttendanceDate,
            CheckInTime    = a.CheckInTime,
            CheckOutTime   = a.CheckOutTime,
            WorkHours      = a.WorkHours,
            Status         = a.Status
        };
    }
}
