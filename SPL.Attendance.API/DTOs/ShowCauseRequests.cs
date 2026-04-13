using System.ComponentModel.DataAnnotations;

namespace SPL.Attendance.API.DTOs
{
    /// <summary>
    /// Request DTO for reviewing a show cause request.
    /// </summary>
    public class ReviewShowCauseRequest
    {
        /// <summary>
        /// Approval status: "Approved" or "Rejected"
        /// </summary>
        [Required(ErrorMessage = "Status is required.")]
        [StringLength(20, MinimumLength = 1, 
            ErrorMessage = "Status must be between 1 and 20 characters.")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Optional review note/comment from supervisor.
        /// </summary>
        [StringLength(500, 
            ErrorMessage = "Review note cannot exceed 500 characters.")]
        public string? ReviewNote { get; set; }
    }

    /// <summary>
    /// Request DTO for submitting a show cause.
    /// </summary>
    public class SubmitShowCauseRequest
    {
        /// <summary>
        /// Reason for multiple sign-in.
        /// </summary>
        [Required(ErrorMessage = "Reason is required.")]
        [StringLength(500, MinimumLength = 5,
            ErrorMessage = "Reason must be between 5 and 500 characters.")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Type of request: "LOGIN" or "LOGOUT"
        /// </summary>
        [StringLength(10, 
            ErrorMessage = "Type cannot exceed 10 characters.")]
        public string Type { get; set; } = "LOGIN";
    }
}
