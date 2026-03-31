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
        public DbSet<AttendanceLog> AttendanceLogs => Set<AttendanceLog>();
        public DbSet<MonthlyAttendanceSummary> MonthlyAttendanceSummaries => Set<MonthlyAttendanceSummary>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Entities.Employee>(entity =>
            {
                entity.HasIndex(e => e.EmployeeCode).IsUnique();

                // Self-referencing: Employee → Supervisor
                entity.HasOne(e => e.Supervisor)
                      .WithMany()
                      .HasForeignKey(e => e.SupervisorId)
                      .OnDelete(DeleteBehavior.Restrict);

                // One Employee → Many Attendances
                entity.HasMany(e => e.Attendances)
                      .WithOne(a => a.Employee)
                      .HasForeignKey(a => a.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Entities.Attendance>(entity =>
            {
                // Enforce business rule: one record per employee per day at DB level
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
