using System.ComponentModel.DataAnnotations;

namespace SPL.Attendance.API.DTOs
{
    public class UpdateOfficeConfigRequest
    {
        [Required(ErrorMessage = "WorkStartTime is required.")]
        public string WorkStartTime { get; set; } = string.Empty; // HH:mm or HH:mm:ss

        [Required(ErrorMessage = "WorkEndTime is required.")]
        public string WorkEndTime { get; set; } = string.Empty; // HH:mm or HH:mm:ss

        public bool RequireApprovalForMultipleLogin { get; set; } = true;

        public bool AutoLogoutAfterShift { get; set; } = true;
    }

    public class OfficeConfigDto
    {
        public int Id { get; set; }
        public string WorkStartTime { get; set; } = string.Empty;
        public string WorkEndTime { get; set; } = string.Empty;
        public bool RequireApprovalForMultipleLogin { get; set; }
        public bool AutoLogoutAfterShift { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}