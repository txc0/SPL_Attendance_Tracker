namespace SPL.Attendance.Business.Models
{
    public class LoginResultDto
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public bool IsSupervisor { get; set; }
        public int? SupervisorId { get; set; }
    }
}