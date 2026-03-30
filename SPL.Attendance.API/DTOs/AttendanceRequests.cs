using System.ComponentModel.DataAnnotations;

namespace SPL.Attendance.API.DTOs
{
    /// <summary>Request body for POST /api/attendance/checkin</summary>
    public class CheckInRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "EmployeeId must be a positive integer.")]
        public int EmployeeId { get; set; }
    }

    /// <summary>Request body for POST /api/attendance/checkout</summary>
    public class CheckOutRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "EmployeeId must be a positive integer.")]
        public int EmployeeId { get; set; }
    }
}
