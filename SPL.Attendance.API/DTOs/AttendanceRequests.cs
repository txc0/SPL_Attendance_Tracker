using System.ComponentModel.DataAnnotations;

namespace SPL.Attendance.API.DTOs
{
    public class CheckInRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "EmployeeId must be a positive integer.")]
        public int EmployeeId { get; set; }
    }

    public class CheckOutRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "EmployeeId must be a positive integer.")]
        public int EmployeeId { get; set; }
    }
}
