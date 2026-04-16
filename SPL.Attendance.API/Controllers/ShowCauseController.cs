using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPL.Attendance.API.DTOs;
using SPL.Attendance.Business.Interfaces;
using SPL.Attendance.Business.Models;
using System.Security.Claims;

namespace SPL.Attendance.API.Controllers
{
    [ApiController]
    [Route("api/showcause")]
    [Produces("application/json")]
    public class ShowCauseController : ControllerBase
    {
        private readonly IShowCauseService _service;
        private readonly ILogger<ShowCauseController> _logger;

        public ShowCauseController(IShowCauseService service, ILogger<ShowCauseController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Employee submits a show cause request for multiple sign-in approval.
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> Submit(
            [FromQuery] int employeeId,
            [FromQuery] string reason,
            [FromQuery] string type = "LOGIN")
        {
            if (employeeId <= 0 || string.IsNullOrWhiteSpace(reason))
                return BadRequest(ApiResponse<object>.Fail("EmployeeId and reason are required."));

            try
            {
                _logger.LogInformation("Show cause submitted for EmployeeId={EmployeeId}, Type={Type}",
                    employeeId, type);

                var result = await _service.SubmitAsync(employeeId, reason, type);
                return Ok(ApiResponse<object>.Ok(
                    "Show cause submitted. Waiting for supervisor approval.",
                    result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting show cause for EmployeeId={EmployeeId}", employeeId);
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Compatibility endpoint for query-string based review calls from frontend.
        /// </summary>
        [HttpPost("review")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReviewRequestByQuery(
            [FromQuery] int showCauseId,
            [FromQuery] int supervisorId,
            [FromQuery] bool isApproved,
            [FromQuery] string? reviewNote)
        {
            if (showCauseId <= 0)
                return BadRequest(ApiResponse<object>.Fail("Valid showCauseId is required."));

            if (supervisorId <= 0)
                return BadRequest(ApiResponse<object>.Fail("Valid supervisorId is required."));

            try
            {
                await _service.ReviewAsync(showCauseId, supervisorId, isApproved, reviewNote);

                return Ok(ApiResponse<object>.Ok(
                    isApproved ? "Request approved." : "Request rejected.",
                    new { requestId = showCauseId, status = isApproved ? "Approved" : "Rejected" }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing show cause RequestId={RequestId}", showCauseId);
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Employee submits a show cause request using email.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("submitbyemail")]
        public async Task<IActionResult> SubmitByEmail(
            [FromQuery] string email,
            [FromQuery] string reason,
            [FromQuery] string type = "LOGIN")
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(reason))
                return BadRequest(ApiResponse<object>.Fail("Email and reason are required."));

            try
            {
                _logger.LogInformation("Show cause submitted via email={Email}, Type={Type}", email, type);

                var result = await _service.SubmitByEmailAsync(email, reason, type);
                return Ok(ApiResponse<object>.Ok(
                    "Show cause submitted. Waiting for supervisor approval.",
                    result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting show cause via email={Email}", email);
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Supervisor approves or rejects a show cause request.
        /// </summary>
        [HttpPost("review/{requestId}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReviewRequest(
            int requestId,
            [FromBody] ReviewShowCauseRequest review)
        {
            if (review == null)
                return BadRequest(ApiResponse<object>.Fail("Review data is required."));

            if (string.IsNullOrEmpty(review.Status) ||
                (review.Status != "Approved" && review.Status != "Rejected"))
                return BadRequest(ApiResponse<object>.Fail(
                    "Status must be 'Approved' or 'Rejected'."));

            try
            {
                //  Convert status string to boolean
                bool isApproved = review.Status == "Approved";

                // Get supervisorId from claims (or default to 1 for now)
                int supervisorId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 1;

                _logger.LogInformation("Reviewing show cause RequestId={RequestId}, Status={Status}, SupervisorId={SupervisorId}",
                    requestId, review.Status, supervisorId);

                // ✅ Call service with correct parameters
                await _service.ReviewAsync(requestId, supervisorId, isApproved, review.ReviewNote);

                // ✅ Provide different messages for approval vs rejection
                var message = isApproved
                    ? "Request approved. Employee can now sign in again."
                    : "Request rejected.";

                return Ok(ApiResponse<object>.Ok(message,
                    new { requestId = requestId, status = review.Status }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing show cause RequestId={RequestId}", requestId);
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Get all pending approval requests for the supervisor (Admin dashboard).
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingRequests()
        {
            try
            {
                // Get supervisorId from claims (or default to 1 for now)
                int supervisorId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 1;

                _logger.LogInformation("Fetching pending requests for SupervisorId={SupervisorId}", supervisorId);

                // ✅ Call the correct method with supervisorId parameter
                var pending = await _service.GetPendingForSupervisorAsync(supervisorId);

                return Ok(ApiResponse<List<ShowCauseRequestDto>>.Ok(
                    $"{pending.Count} pending request(s).",
                    pending));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending requests");
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Check if an employee has a pending show cause request.
        /// </summary>
        [HttpGet("employee/{employeeId:int}")]
        public async Task<IActionResult> GetEmployeePending(
            [FromRoute] int employeeId)
        {
            if (employeeId <= 0)
                return BadRequest(ApiResponse<object>.Fail("Invalid employee ID."));

            try
            {
                _logger.LogInformation("Fetching pending request for EmployeeId={EmployeeId}", employeeId);

                var result = await _service.GetPendingForEmployeeAsync(employeeId);

                return Ok(ApiResponse<object>.Ok(
                    result == null ? "No pending requests." : "Pending request found.",
                    result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending request for EmployeeId={EmployeeId}", employeeId);
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [AllowAnonymous]
        [HttpGet("pending/{supervisorId:int}")]
        public async Task<IActionResult> GetPending(
    [FromRoute] int supervisorId)
        {
            var list = await _service.GetPendingForSupervisorAsync(supervisorId);
            return Ok(ApiResponse<object>.Ok(
                $"{list.Count} pending request(s).", list));
        }
    }
}