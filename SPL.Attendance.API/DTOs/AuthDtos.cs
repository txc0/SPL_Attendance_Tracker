using System.ComponentModel.DataAnnotations;

namespace SPL.Attendance.API.DTOs
{
    /// <summary>Request body for POST /api/auth/login</summary>
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Returned after successful login</summary>
    public class LoginResponse
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public bool IsSupervisor { get; set; }
        public int? SupervisorId { get; set; }
        public string Token { get; set; } = string.Empty;
    }
}