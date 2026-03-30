using Microsoft.EntityFrameworkCore;
using SPL.Attendance.Data.Entities;

namespace SPL.Attendance.Data.Context
{
    public class SPLAttendanceDbContext : DbContext
    {
        public SPLAttendanceDbContext(DbContextOptions<SPLAttendanceDbContext> options)
            : base(options) { }

        public DbSet<Entities.Employee> Employees => Set<Entities.Employee>();
        public DbSet<Entities.Attendance> Attendances => Set<Entities.Attendance>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Entities.Employee>(entity =>
            {
                entity.HasIndex(e => e.EmployeeCode).IsUnique();

                entity.HasOne(e => e.Supervisor)
                      .WithMany()
                      .HasForeignKey(e => e.SupervisorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Attendances)
                      .WithOne(a => a.Employee)
                      .HasForeignKey(a => a.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Entities.Attendance>(entity =>
            {
                entity.HasIndex(a => new { a.EmployeeId, a.AttendanceDate })
                      .IsUnique()
                      .HasDatabaseName("UX_Attendance_Employee_Date");
            });

            modelBuilder.Entity<Entities.Employee>().HasData(
                new Entities.Employee
                {
                    Id = 1,
                    EmployeeCode = "EMP001",
                    Name = "Demo Employee",
                    Email = "demo@spl.com",
                    IsActive = true
                }
            );
        }
    }
}
