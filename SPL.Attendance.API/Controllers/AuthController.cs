using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SPL.Attendance.API.DTOs;
using SPL.Attendance.Business.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SPL.Attendance.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService,
                              IConfiguration config,
                              ILogger<AuthController> logger)
        {
            _authService = authService;
            _config = config;
            _logger = logger;
        }

        /// <summary>Login with email and password. Returns JWT token.</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid request."));

            _logger.LogInformation("Login attempt for email={Email}", request.Email);

            var result = await _authService.LoginAsync(request.Email, request.Password);

            // Generate JWT token
            var token = GenerateToken(result.EmployeeId, result.Name,
                                      result.IsSupervisor);

            var response = new LoginResponse
            {
                EmployeeId = result.EmployeeId,
                Name = result.Name,
                Email = result.Email,
                EmployeeCode = result.EmployeeCode,
                IsSupervisor = result.IsSupervisor,
                SupervisorId = result.SupervisorId,
                Token = token
            };

            return Ok(ApiResponse<LoginResponse>.Ok(
                $"Welcome back, {result.Name}!", response));
        }

        /// <summary>
        /// Set or reset password for an employee.
        /// Use this endpoint to set initial passwords.
        /// </summary>
        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword(
            [FromQuery] int employeeId,
            [FromQuery] string password)
        {
            await _authService.SetPasswordAsync(employeeId, password);
            return Ok(ApiResponse<object>.Ok(
                $"Password set successfully for employee {employeeId}."));
        }

        // ── Private: JWT generation ──────────────────────
        private string GenerateToken(int employeeId, string name,
                                     bool isSupervisor)
        {
            var key = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key,
                            SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddHours(
                            double.Parse(_config["Jwt:ExpiryHours"]!));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, employeeId.ToString()),
                new Claim(ClaimTypes.Name,           name),
                new Claim("IsSupervisor",            isSupervisor.ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}