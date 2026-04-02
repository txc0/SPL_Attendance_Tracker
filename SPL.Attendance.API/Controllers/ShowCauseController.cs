using Microsoft.AspNetCore.Mvc;
using SPL.Attendance.API.DTOs;
using SPL.Attendance.Business.Interfaces;

namespace SPL.Attendance.API.Controllers
{
    [ApiController]
    [Route("api/showcause")]
    [Produces("application/json")]
    public class ShowCauseController : ControllerBase
    {
        private readonly IShowCauseService _service;

        public ShowCauseController(IShowCauseService service)
        {
            _service = service;
        }

        /// <summary>Employee submits a show cause reason.</summary>
        [HttpPost("submit")]
        public async Task<IActionResult> Submit(
            [FromQuery] int employeeId,
            [FromQuery] string reason)
        {
            var result = await _service.SubmitAsync(employeeId, reason);
            return Ok(ApiResponse<object>.Ok(
                "Show cause submitted. Waiting for supervisor approval.",
                result));
        }

        /// <summary>
        /// Supervisor approves or rejects a show cause.
        /// </summary>
        [HttpPost("review")]
        public async Task<IActionResult> Review(
            [FromQuery] int showCauseId,
            [FromQuery] int supervisorId,
            [FromQuery] bool isApproved,
            [FromQuery] string? reviewNote = null)
        {
            await _service.ReviewAsync(
                showCauseId, supervisorId, isApproved, reviewNote);

            var msg = isApproved
                ? "Show cause approved. Employee can now check in."
                : "Show cause rejected.";

            return Ok(ApiResponse<object>.Ok(msg));
        }

        /// <summary>Get all pending show causes for a supervisor.</summary>
        [HttpGet("pending/{supervisorId:int}")]
        public async Task<IActionResult> GetPending(
            [FromRoute] int supervisorId)
        {
            var list = await _service.GetPendingForSupervisorAsync(supervisorId);
            return Ok(ApiResponse<object>.Ok(
                $"{list.Count} pending request(s).", list));
        }

        /// <summary>Check if employee has a pending show cause.</summary>
        [HttpGet("employee/{employeeId:int}")]
        public async Task<IActionResult> GetEmployeePending(
            [FromRoute] int employeeId)
        {
            var result = await _service.GetPendingForEmployeeAsync(employeeId);
            return Ok(ApiResponse<object>.Ok(
                result == null ? "No pending requests." : "Pending request found.",
                result));
        }
    }
}