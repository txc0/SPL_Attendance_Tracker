using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DAL.EF.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        [ForeignKey("Employee")]
        public int EmployeeId { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly Check_in_Time { get; set; }
        public TimeOnly Check_out_time { get; set; }

        public double work_time { get; set; } 

        public bool status { get; set; }

        public virtual Employee Employee { get; set; }


    }
}
