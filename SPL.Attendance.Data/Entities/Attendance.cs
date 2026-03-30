using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPL.Attendance.Data.Entities
{
    /// <summary>
    /// Represents a single attendance record (one per employee per calendar day).
    /// </summary>
    [Table("Attendances")]
    public class Attendance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        /// <summary>
        /// Calendar date of attendance. Unique per employee per day.
        /// </summary>
        [Required]
        [Column(TypeName = "date")]
        public DateTime AttendanceDate { get; set; }

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        /// <summary>
        /// Calculated on check-out: (CheckOutTime - CheckInTime).TotalHours
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal? WorkHours { get; set; }

        /// <summary>
        /// Attendance status: Present | Late | HalfDay | Absent
        /// </summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Present";

        // ── Navigation property ──────────────────────────────────────────────
        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee Employee { get; set; } = null!;
    }
}
