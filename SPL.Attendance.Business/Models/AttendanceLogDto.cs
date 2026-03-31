namespace SPL.Attendance.Business.Models
{
    public class AttendanceLogDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public DateTime LogDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}