using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPL.Attendance.Data.Entities
{
    [Table("MonthlyAttendanceSummary")]
    public class MonthlyAttendanceSummary
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public int TotalDays { get; set; } = 0;

        public bool IsReset { get; set; } = false;

        public DateTime? ResetAt { get; set; }

        [MaxLength(100)]
        public string? ResetByManager { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee Employee { get; set; } = null!;
    }
}