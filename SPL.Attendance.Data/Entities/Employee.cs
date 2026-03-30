using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPL.Attendance.Data.Entities
{
    /// <summary>
    /// Represents an employee in the SPL system.
    /// </summary>
    [Table("Employees")]
    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Email { get; set; }

        /// <summary>
        /// Self-referencing FK: the employee's direct supervisor.
        /// </summary>
        public int? SupervisorId { get; set; }

        public bool IsActive { get; set; } = true;

        // ── Navigation properties ────────────────────────────────────────────
        [ForeignKey(nameof(SupervisorId))]
        public virtual Employee? Supervisor { get; set; }

        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}
