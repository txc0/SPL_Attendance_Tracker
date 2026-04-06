namespace SPL.Attendance.Business.Models
{
    public class EmployeeDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int? SupervisorId { get; set; }
        public string? SupervisorName { get; set; }
        public bool IsActive { get; set; }
        public bool IsSupervisor { get; set; }
    }

    public class CreateEmployeeDto
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int? SupervisorId { get; set; }
        public bool IsSupervisor { get; set; } = false;
    }

    public class UpdateEmployeeDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int? SupervisorId { get; set; }
        public bool IsSupervisor { get; set; } = false;
    }
}
