using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DAL.EF.Models
{
    internal class OfficeSetting
    {
        public TimeOnly Start_time { get; set; }
        public TimeOnly End_time { get; set; }
        public int daily_working_hours { get; set; }

        public DateTime Created_At { get; set; }

        [ForeignKey("Employee")]
        public int Created_by { get; set; }
        public virtual Employee Employee { get; set; }
    }
}
