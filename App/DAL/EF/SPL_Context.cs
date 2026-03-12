using System;
using System.Collections.Generic;
using System.Text;
using DAL.EF.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.EF
{
    public class SPL_Context : DbContext
    {
        public SPL_Context(DbContextOptions<SPL_Context> options) : base(options)
        {
        }
        public DbSet <Employee> Employees { get; set; }
        public DbSet <Attendance> Attendances { get; set; }

        public DbSet <OfficeSetting> OfficeSettings { get; set; }
    }
}
