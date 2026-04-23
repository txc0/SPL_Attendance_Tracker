using System.ComponentModel.DataAnnotations;

namespace SPL.Attendance.API.DTOs
{
    public class CreateEmployeeRequest
    {
        [Required(ErrorMessage = "EmployeeCode is required.")]
        [MaxLength(50, ErrorMessage = "EmployeeCode cannot exceed 50 characters.")]
        [RegularExpression(@"^SPL_\d+$",
            ErrorMessage = "EmployeeCode may only contain letters, numbers, hyphens, and underscores.")]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email is not a valid email address.")]
           [RegularExpression(@"^[A-Za-z0-9._%+-]+@splbdgroup\.com$",
            ErrorMessage = "Email must be your company Email name@splbdgroup.com.")]
        [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string? Email { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "SupervisorId must be a positive integer.")]
        public int? SupervisorId { get; set; }

        public bool IsSupervisor { get; set; } = false;
    }
    public class UpdateEmployeeRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email is not a valid email address.")]
        [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string? Email { get; set; }

        public bool IsSupervisor { get; set; } = false;

        [Range(1, int.MaxValue, ErrorMessage = "SupervisorId must be a positive integer.")]
        public int? SupervisorId { get; set; }

    }
}
