using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPL.Attendance.Data.Entities
{
    [Table("ShowCauseRequests")]
    public class ShowCauseRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public int SupervisorId { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        // Pending | Approved | Rejected
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime RequestedAt { get; set; } = DateTime.Now;

        public DateTime? ReviewedAt { get; set; }

        [MaxLength(255)]
        public string? ReviewNote { get; set; }

        // ── Navigation properties ────────────────────────
        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee Employee { get; set; } = null!;

        [ForeignKey(nameof(SupervisorId))]
        public virtual Employee Supervisor { get; set; } = null!;
    }
}