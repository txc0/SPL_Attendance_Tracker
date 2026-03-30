namespace SPL.Attendance.Business.Models
{
    /// <summary>
    /// Data Transfer Object returned by the Business Layer to the API Layer.
    /// Decouples the API from the Data entities.
    /// </summary>
    public class AttendanceRecordDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public decimal? WorkHours { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
