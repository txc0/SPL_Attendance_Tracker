using SPL.Attendance.Business.Interfaces;
using SPL.Attendance.Business.Models;
using SPL.Attendance.Data.Entities;
using SPL.Attendance.Data.Repositories;

namespace SPL.Attendance.Business.Services
{
    /// <summary>
    /// Implements all Sprint-1 business rules for attendance management.
    /// This layer has zero knowledge of HTTP or database technology.
    /// </summary>
    public class AttendanceService : IAttendanceService
    {
        private readonly IAttendanceRepository _repository;

        public AttendanceService(IAttendanceRepository repository)
        {
            _repository = repository;
        }

        // ────────────────────────────────────────────────────────────────────
        // CHECK-IN
        // ────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task CheckInAsync(int employeeId)
        {
            // Guard: employee must exist and be active
            if (!await _repository.EmployeeExistsAsync(employeeId))
                throw new KeyNotFoundException($"Employee with ID {employeeId} was not found or is inactive.");

            var today = DateTime.Today;
            var existing = await _repository.GetAttendanceAsync(employeeId, today);

            // Business Rule: No duplicate check-in on the same calendar day
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

        // ────────────────────────────────────────────────────────────────────
        // CHECK-OUT
        // ────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task CheckOutAsync(int employeeId)
        {
            // Guard: employee must exist and be active
            if (!await _repository.EmployeeExistsAsync(employeeId))
                throw new KeyNotFoundException($"Employee with ID {employeeId} was not found or is inactive.");

            var today = DateTime.Today;
            var attendance = await _repository.GetAttendanceAsync(employeeId, today);

            // Business Rule: Must have a check-in before checking out
            if (attendance == null)
                throw new InvalidOperationException(
                    $"Employee {employeeId} has no check-in record for today ({today:dd-MMM-yyyy}). " +
                    "Check-out requires a valid check-in.");

            // Business Rule: Cannot check out twice
            if (attendance.CheckOutTime != null)
                throw new InvalidOperationException(
                    $"Employee {employeeId} has already checked out today ({today:dd-MMM-yyyy}). " +
                    "Duplicate check-out is not permitted.");

            attendance.CheckOutTime = DateTime.Now;

            // Work hours calculation: stored as decimal (e.g. 8.50 = 8h 30m)
            attendance.WorkHours = (decimal)(attendance.CheckOutTime - attendance.CheckInTime)!
                                   .Value.TotalHours;

            // Optional status refinement (Sprint-2 will expand this logic)
            attendance.WorkHours = Math.Round(attendance.WorkHours.Value, 2);

            await _repository.UpdateCheckOutAsync(attendance);
        }

        // ────────────────────────────────────────────────────────────────────
        // QUERIES
        // ────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<List<AttendanceRecordDto>> GetAttendanceHistoryAsync(int employeeId)
        {
            var records = await _repository.GetHistoryAsync(employeeId);
            return records.Select(MapToDto).ToList();
        }

        /// <inheritdoc />
        public async Task<AttendanceRecordDto?> GetAttendanceByDateAsync(int employeeId, DateTime date)
        {
            var record = await _repository.GetByDateAsync(employeeId, date);
            return record == null ? null : MapToDto(record);
        }

        // ────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ────────────────────────────────────────────────────────────────────

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
