using Microsoft.AspNetCore.Mvc;
using SPL.Attendance.API.DTOs;
using SPL.Attendance.Data.Entities;
using SPL.Attendance.Data.Repositories;
using System.Globalization;

namespace SPL.Attendance.API.Controllers
{
    [ApiController]
    [Route("api/office-config")]
    [Produces("application/json")]
    public class OfficeConfigController : ControllerBase
    {
        private readonly ICompanyPolicyRepository _policyRepository;
        private readonly ILogger<OfficeConfigController> _logger;

        public OfficeConfigController(
            ICompanyPolicyRepository policyRepository,
            ILogger<OfficeConfigController> logger)
        {
            _policyRepository = policyRepository;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<OfficeConfigDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get()
        {
            var policy = await _policyRepository.GetActiveAsync();
            if (policy == null)
                return NotFound(ApiResponse<object>.Fail("No active office configuration found."));

            return Ok(ApiResponse<OfficeConfigDto>.Ok(
                "Office configuration retrieved successfully.",
                Map(policy)));
        }

        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<OfficeConfigDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Put([FromBody] UpdateOfficeConfigRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.Fail(
                    "Validation failed: " + string.Join(" | ", errors)));
            }

            if (!TryParseTime(request.WorkStartTime, out var workStartTime))
                return BadRequest(ApiResponse<object>.Fail("Invalid WorkStartTime. Use HH:mm or HH:mm:ss."));

            if (!TryParseTime(request.WorkEndTime, out var workEndTime))
                return BadRequest(ApiResponse<object>.Fail("Invalid WorkEndTime. Use HH:mm or HH:mm:ss."));

            if (workEndTime <= workStartTime)
                return BadRequest(ApiResponse<object>.Fail("WorkEndTime must be later than WorkStartTime."));

            var updated = await _policyRepository.UpsertActiveAsync(new CompanyPolicy
            {
                WorkStartTime = workStartTime,
                WorkEndTime = workEndTime,
                RequireApprovalForMultipleLogin = request.RequireApprovalForMultipleLogin,
                AutoLogoutAfterShift = request.AutoLogoutAfterShift,
                IsActive = true
            });

            _logger.LogInformation("Office configuration updated. PolicyId={PolicyId}", updated.Id);

            return Ok(ApiResponse<OfficeConfigDto>.Ok(
                "Office configuration updated successfully.",
                Map(updated)));
        }

        private static bool TryParseTime(string value, out TimeSpan time)
        {
            return TimeSpan.TryParseExact(value, "hh\\:mm", CultureInfo.InvariantCulture, out time)
                   || TimeSpan.TryParseExact(value, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out time);
        }

        private static OfficeConfigDto Map(CompanyPolicy policy)
        {
            return new OfficeConfigDto
            {
                Id = policy.Id,
                WorkStartTime = policy.WorkStartTime.ToString(@"hh\:mm"),
                WorkEndTime = policy.WorkEndTime.ToString(@"hh\:mm"),
                RequireApprovalForMultipleLogin = policy.RequireApprovalForMultipleLogin,
                AutoLogoutAfterShift = policy.AutoLogoutAfterShift,
                IsActive = policy.IsActive,
                CreatedAt = policy.CreatedAt,
                UpdatedAt = policy.UpdatedAt
            };
        }
    }
}