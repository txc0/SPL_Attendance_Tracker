using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.EF.Models
{
    public class Employee
    {
        [Required]
        public int EmployeeId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public int SupervisorId { get; set; }

        public TimeOnly Created_at { get; set; }

        [ForeignKey("Superviso")]
        public int AttendanceId { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; }
        public bool IsActive { get; internal set; }
    }
}
