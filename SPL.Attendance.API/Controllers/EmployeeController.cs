using Microsoft.AspNetCore.Mvc;
using SPL.Attendance.API.DTOs;
using SPL.Attendance.Business.Interfaces;
using SPL.Attendance.Business.Models;

namespace SPL.Attendance.API.Controllers
{
    [ApiController]
    [Route("api/employees")]
    [Produces("application/json")]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _service;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(IEmployeeService service,
                                  ILogger<EmployeeController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<EmployeeDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("GetAll employees requested.");
            var employees = await _service.GetAllEmployeesAsync();
            return Ok(ApiResponse<List<EmployeeDto>>.Ok(
                $"{employees.Count} employee(s) retrieved.", employees));
        }


        /// <summary>Returns a single employee by their ID.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            _logger.LogInformation("GetById requested for EmployeeId={Id}", id);
            var employee = await _service.GetEmployeeByIdAsync(id);
            return Ok(ApiResponse<EmployeeDto>.Ok("Employee retrieved successfully.", employee));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<List<string>>.Fail(
                    "Validation failed: " + string.Join(" | ", errors)));
            }

            _logger.LogInformation("Create employee requested. Code={Code}", request.EmployeeCode);

            var dto = new CreateEmployeeDto
            {
                EmployeeCode = request.EmployeeCode,
                Name         = request.Name,
                Email        = request.Email,
                SupervisorId = request.SupervisorId
            };

            var created = await _service.CreateEmployeeAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                ApiResponse<EmployeeDto>.Ok(
                    $"Employee '{created.Name}' (Code: {created.EmployeeCode}) created successfully.",
                    created));
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update([FromRoute] int id,
                                                [FromBody] UpdateEmployeeRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<List<string>>.Fail(
                    "Validation failed: " + string.Join(" | ", errors)));
            }

            _logger.LogInformation("Update employee requested. EmployeeId={Id}", id);

            var dto = new UpdateEmployeeDto
            {
                Name         = request.Name,
                Email        = request.Email,
                SupervisorId = request.SupervisorId
            };

            var updated = await _service.UpdateEmployeeAsync(id, dto);

            return Ok(ApiResponse<EmployeeDto>.Ok(
                $"Employee '{updated.Name}' updated successfully.", updated));
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate([FromRoute] int id)
        {
            _logger.LogInformation("Deactivate employee requested. EmployeeId={Id}", id);
            await _service.DeactivateEmployeeAsync(id);
            return Ok(ApiResponse<object>.Ok(
                $"Employee {id} has been deactivated. Attendance records are preserved."));
        }
    }
}
