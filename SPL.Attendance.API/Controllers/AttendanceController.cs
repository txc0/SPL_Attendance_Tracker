using Microsoft.AspNetCore.Mvc;
using SPL.Attendance.API.DTOs;
using SPL.Attendance.Business.Interfaces;

namespace SPL.Attendance.API.Controllers
{
    [ApiController]
    [Route("api/attendance")]
    [Produces("application/json")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _service;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(IAttendanceService service,
                                    ILogger<AttendanceController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        // ────────────────────────────────────────────────────────────────────
        // POST /api/attendance/checkin
        // ────────────────────────────────────────────────────────────────────

        /// <summary>Records an employee check-in for today.</summary>
        /// <remarks>
        /// An employee may check in only once per calendar day.
        /// Returns 400 if a check-in record already exists for today.
        /// </remarks>
        [HttpPost("checkin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid request payload."));

            _logger.LogInformation("Check-in request received for EmployeeId={EmployeeId}", request.EmployeeId);

            await _service.CheckInAsync(request.EmployeeId);

            return Ok(ApiResponse<object>.Ok($"Employee {request.EmployeeId} checked in successfully at {DateTime.Now:HH:mm:ss}."));
        }


        /// <summary>Records an employee check-out for today and calculates work hours.</summary>
        /// <remarks>
        /// Requires a prior check-in for today. Work hours are calculated and persisted.
        /// Returns 400 if there is no check-in or if the employee already checked out.
        /// </remarks>
        [HttpPost("checkout")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid request payload."));

            _logger.LogInformation("Check-out request received for EmployeeId={EmployeeId}", request.EmployeeId);

            await _service.CheckOutAsync(request.EmployeeId);

            return Ok(ApiResponse<object>.Ok($"Employee {request.EmployeeId} checked out successfully at {DateTime.Now:HH:mm:ss}."));
        }

        [HttpGet("{employeeId:int}")]
        [ProducesResponseType(typeof(ApiResponse<List<Business.Models.AttendanceRecordDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistory([FromRoute] int employeeId)
        {
            _logger.LogInformation("Attendance history requested for EmployeeId={EmployeeId}", employeeId);

            var records = await _service.GetAttendanceHistoryAsync(employeeId);
            return Ok(ApiResponse<List<Business.Models.AttendanceRecordDto>>.Ok(
                $"Retrieved {records.Count} attendance record(s) for employee {employeeId}.",
                records));
        }

        /// <summary>Returns the attendance record for a specific employee on a specific date.</summary>
        /// <param name="employeeId">Employee identifier.</param>
        /// <param name="date">Date in yyyy-MM-dd format (e.g. 2025-07-01).</param>
        [HttpGet("{employeeId:int}/{date}")]
        [ProducesResponseType(typeof(ApiResponse<Business.Models.AttendanceRecordDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByDate([FromRoute] int employeeId, [FromRoute] string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
                return BadRequest(ApiResponse<object>.Fail("Invalid date format. Use yyyy-MM-dd."));

            _logger.LogInformation("Attendance by date requested for EmployeeId={EmployeeId}, Date={Date}",
                                   employeeId, parsedDate.ToString("yyyy-MM-dd"));

            var record = await _service.GetAttendanceByDateAsync(employeeId, parsedDate);

            if (record == null)
                return NotFound(ApiResponse<object>.Fail(
                    $"No attendance record found for employee {employeeId} on {parsedDate:dd-MMM-yyyy}."));

            return Ok(ApiResponse<Business.Models.AttendanceRecordDto>.Ok(
                "Attendance record retrieved successfully.", record));
        }
    }
}
