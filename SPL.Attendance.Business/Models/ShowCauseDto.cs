namespace SPL.Attendance.Business.Models
{
    public class ShowCauseRequestDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int SupervisorId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }
    }

    public class SubmitShowCauseDto
    {
        public int EmployeeId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ReviewShowCauseDto
    {
        public int ShowCauseId { get; set; }
        public int SupervisorId { get; set; }
        public bool IsApproved { get; set; }
        public string? ReviewNote { get; set; }
    }
}