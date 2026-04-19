using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPL.Attendance.Data.Entities
{
    [Table("CompanyPolicies")]
    public class CompanyPolicy
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column(TypeName = "time")]
        public TimeSpan WorkStartTime { get; set; } = new(8, 0, 0);

        [Column(TypeName = "time")]
        public TimeSpan WorkEndTime { get; set; } = new(16, 0, 0);

        public bool RequireApprovalForMultipleLogin { get; set; } = true;

        public bool AutoLogoutAfterShift { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}