using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPL.Attendance.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            // ── Employees table ─────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy",
                                    MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeCode = table.Column<string>(type: "varchar(50)", maxLength: 50,
                                                        nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100,
                                                nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150,
                                                 nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SupervisorId = table.Column<int>(type: "int", nullable: true),
                    IsActive     = table.Column<bool>(type: "tinyint(1)", nullable: false,
                                                      defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name:       "FK_Employees_Supervisor",
                        column:     x => x.SupervisorId,
                        principalTable:  "Employees",
                        principalColumn: "Id",
                        onDelete:   ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // ── Attendances table ───────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy",
                                    MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId     = table.Column<int>(type: "int",       nullable: false),
                    AttendanceDate = table.Column<DateTime>(type: "date", nullable: false),
                    CheckInTime    = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CheckOutTime   = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WorkHours      = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Status         = table.Column<string>(type: "varchar(20)", maxLength: 20,
                                                          nullable: false,
                                                          defaultValue: "Present")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                    table.ForeignKey(
                        name:       "FK_Attendances_Employees_EmployeeId",
                        column:     x => x.EmployeeId,
                        principalTable:  "Employees",
                        principalColumn: "Id",
                        onDelete:   ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // ── Indexes ─────────────────────────────────────────────────────
            migrationBuilder.CreateIndex(
                name:    "IX_Employees_EmployeeCode",
                table:   "Employees",
                column:  "EmployeeCode",
                unique:  true);

            migrationBuilder.CreateIndex(
                name:    "IX_Employees_SupervisorId",
                table:   "Employees",
                column:  "SupervisorId");

            migrationBuilder.CreateIndex(
                name:    "UX_Attendance_Employee_Date",
                table:   "Attendances",
                columns: new[] { "EmployeeId", "AttendanceDate" },
                unique:  true);

            // ── Seed: Demo Employee (Id = 1) ────────────────────────────────
            migrationBuilder.InsertData(
                table:   "Employees",
                columns: new[] { "Id", "EmployeeCode", "Name", "Email", "SupervisorId", "IsActive" },
                values:  new object[] { 1, "EMP001", "Demo Employee", "demo@spl.com", null, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Attendances");
            migrationBuilder.DropTable(name: "Employees");
        }
    }
}
