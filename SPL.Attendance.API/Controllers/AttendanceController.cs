using Microsoft.AspNetCore.Mvc;
using SPL.Attendance.API.DTOs;
using SPL.Attendance.Business.Interfaces;
using SPL.Attendance.Business.Models;

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

        // GET /api/attendance/{employeeId}/logs
        [HttpGet("{employeeId:int}/logs")]
        public async Task<IActionResult> GetLogs([FromRoute] int employeeId)
        {
            var logs = await _service.GetLogsAsync(employeeId);
            return Ok(ApiResponse<List<AttendanceLogDto>>.Ok(
                $"{logs.Count} log(s) found for employee {employeeId}.", logs));
        }

        // GET /api/attendance/{employeeId}/logs/{date}
        [HttpGet("{employeeId:int}/logs/{date}")]
        public async Task<IActionResult> GetLogsByDate(
            [FromRoute] int employeeId,
            [FromRoute] string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
                return BadRequest(ApiResponse<object>.Fail(
                    "Invalid date format. Use yyyy-MM-dd."));

            var logs = await _service.GetLogsByDateAsync(employeeId, parsedDate);
            return Ok(ApiResponse<List<AttendanceLogDto>>.Ok(
                $"{logs.Count} log(s) found for employee {employeeId} on {parsedDate:dd-MMM-yyyy}.",
                logs));
        }
    }
}
