namespace SPL.Attendance.Business.Models
{
    public class MonthlyAttendanceSummaryDto
    {
        public int EmployeeId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int TotalDays { get; set; }
        public bool IsReset { get; set; }
        public DateTime? ResetAt { get; set; }
        public string? ResetByManager { get; set; }
    }
}