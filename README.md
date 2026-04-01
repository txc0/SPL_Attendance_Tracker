# SPL Attendance Management System

> **Sprint 1 — Core Attendance Logic**
> ASP.NET Core 8 · Entity Framework Core 8 · Pomelo MySQL Provider · xUnit · Swagger

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Technology Stack](#technology-stack)
3. [Architecture](#architecture)
4. [Project Structure](#project-structure)
5. [Getting Started](#getting-started)
6. [Database Setup](#database-setup)
7. [Running the API](#running-the-api)
8. [API Endpoints](#api-endpoints)
9. [Running Tests](#running-tests)
10. [Git Branching Strategy](#git-branching-strategy)
11. [Commit Message Convention](#commit-message-convention)
12. [Sprint Roadmap](#sprint-roadmap)

---

## Project Overview

The **SPL Attendance Management System** is an enterprise-grade web application for managing employee attendance, movement tracking, and supervisor approvals within SPL.

This system is built following the **Scrum / Agile SDLC** methodology. Each sprint represents one full mini-cycle of analysis, design, implementation, and testing.

**Sprint 1** delivers the core attendance lifecycle:
- Employee **Check-In** — multiple times per day allowed, every event logged
- Employee **Check-Out** — closes the latest open check-in, calculates cumulative work hours
- **Employee Management** — Create, Read, Update, Deactivate (soft delete)
- **Attendance Logs** — every single check-in and check-out event stored in a dedicated log table
- **Monthly Attendance Count** — counts completed days (full check-in + check-out cycle) per month
- **Manager Reset** — manager can reset any employee's monthly attendance count

---

## Technology Stack

| Layer | Technology |
|---|---|
| Backend Framework | ASP.NET Core 9 (Web API) |
| ORM | Entity Framework Core 9 (Code-First) |
| Database | MySQL 8.x (Pomelo EF Core provider) |
| Dependency Injection | Built-in ASP.NET Core DI |
| API Documentation | Swagger / Swashbuckle |
| Unit Testing | xUnit + Moq + FluentAssertions |
| Version Control | Git + GitHub (Git Flow) |
| IDE | Visual Studio 2022 |

---

## Architecture

The system follows a strict **3-Tier Architecture**. No layer ever bypasses another.

```
┌──────────────────────────────────────┐
│   React Frontend (Sprint 3+)         │
│   POST /api/attendance/checkin       │
└──────────────────┬───────────────────┘
                   │ HTTP
┌──────────────────▼───────────────────┐
│  APPLICATION LAYER                   │
│  SPL.Attendance.API                  │
│  AttendanceController.cs             │
│  EmployeeController.cs               │
│  (Receives HTTP, validates input,    │
│   delegates to service — NO BL)      │
└──────────────────┬───────────────────┘
                   │ IAttendanceService / IEmployeeService
┌──────────────────▼───────────────────┐
│  BUSINESS LOGIC LAYER                │
│  SPL.Attendance.Business             │
│  AttendanceService.cs                │
│  EmployeeService.cs                  │
│  (All business rules live here)      │
└──────────────────┬───────────────────┘
                   │ IAttendanceRepository / IEmployeeRepository
┌──────────────────▼───────────────────┐
│  DATA ACCESS LAYER                   │
│  SPL.Attendance.Data                 │
│  AttendanceRepository.cs             │
│  EmployeeRepository.cs               │
│  (EF Core + MySQL — NO BL here)      │
└──────────────────┬───────────────────┘
                   │
┌──────────────────▼───────────────────┐
│  MySQL 8.x Database (spldb)          │
│  Employees              Attendances  │
│  AttendanceLogs         Monthly      │
│                         Attendance   │
│                         Summary      │
└──────────────────────────────────────┘
```

---

## Project Structure

```
SPL.AttendanceManagementSystem.sln
│
├── SPL.Attendance.API/                        ← Application Layer (Web API)
│   ├── Controllers/
│   │   ├── AttendanceController.cs
│   │   └── EmployeeController.cs
│   ├── DTOs/
│   │   ├── AttendanceRequests.cs              ← CheckInRequest, CheckOutRequest
│   │   ├── EmployeeRequests.cs                ← CreateEmployeeRequest, UpdateEmployeeRequest
│   │   └── ApiResponse.cs                     ← Standard { success, message, data } envelope
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs      ← Global error handler
│   ├── Program.cs                              ← DI + Swagger + CORS + EF setup
│   ├── appsettings.json
│   └── appsettings.Example.json               ← Safe template — commit this, not appsettings.json
│
├── SPL.Attendance.Business/                   ← Business Logic Layer
│   ├── Interfaces/
│   │   ├── IAttendanceService.cs
│   │   └── IEmployeeService.cs
│   ├── Models/
│   │   ├── AttendanceRecordDto.cs
│   │   ├── AttendanceLogDto.cs
│   │   └── EmployeeDtos.cs
│   └── Services/
│       ├── AttendanceService.cs
│       └── EmployeeService.cs
│
├── SPL.Attendance.Data/                       ← Data Access Layer
│   ├── Context/
│   │   └── SPLAttendanceDbContext.cs
│   ├── Entities/
│   │   ├── Employee.cs
│   │   ├── Attendance.cs
│   │   ├── AttendanceLog.cs
│   │   └── MonthlyAttendanceSummary.cs
│   ├── Repositories/
│   │   ├── IAttendanceRepository.cs
│   │   ├── AttendanceRepository.cs
│   │   ├── IEmployeeRepository.cs
│   │   └── EmployeeRepository.cs
│   └── Migrations/
│       ├── 20250101000000_InitialCreate.cs
│       └── SPLAttendanceDbContextModelSnapshot.cs
│
├── SPL.Attendance.Tests/                      ← xUnit Unit Tests
│   ├── AttendanceServiceTests.cs              ← TC-001 through TC-006
│   └── EmployeeServiceTests.cs               ← Employee business rule tests
│
├── SPL_Attendance_Sprint1.postman_collection.json
├── global.json                                ← Pins .NET SDK to 9.0.312
├── README.md
└── .gitignore
```

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [MySQL 8.x](https://dev.mysql.com/downloads/mysql/) running locally on port 3306
- [Visual Studio 2022](https://visualstudio.microsoft.com/) with **ASP.NET and web development** workload
- [Git](https://git-scm.com/)

### 1. Clone the repository

```bash
git clone https://github.com/YourOrg/SPL.AttendanceManagementSystem.git
cd SPL.AttendanceManagementSystem
git checkout develop
git checkout -b feature/sprint1-checkin-checkout
```

### 2. Configure the database connection

Open `SPL.Attendance.API/appsettings.json` and update with your MySQL credentials:

```json
{
  "ConnectionStrings": {
    "SPLAttendanceDB": "server=localhost;port=3306;database=spldb;uid=root;password=YOUR_PASSWORD;"
  }
}
```

> Make sure `database=` matches exactly the schema name shown in MySQL Workbench.

---

## Database Setup

Run this full SQL script in **MySQL Workbench**:

```sql
USE spldb;

-- Employees table
CREATE TABLE IF NOT EXISTS `Employees` (
    `Id`           INT           NOT NULL AUTO_INCREMENT,
    `EmployeeCode` VARCHAR(50)   NOT NULL,
    `Name`         VARCHAR(100)  NOT NULL,
    `Email`        VARCHAR(150)  NULL,
    `SupervisorId` INT           NULL,
    `IsActive`     TINYINT(1)    NOT NULL DEFAULT 1,
    CONSTRAINT `PK_Employees` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Employees_Supervisor`
        FOREIGN KEY (`SupervisorId`) REFERENCES `Employees`(`Id`)
        ON DELETE RESTRICT
);
CREATE UNIQUE INDEX `IX_Employees_EmployeeCode` ON `Employees` (`EmployeeCode`);
CREATE INDEX `IX_Employees_SupervisorId` ON `Employees` (`SupervisorId`);

-- Attendances table (one summary row per employee per day)
CREATE TABLE IF NOT EXISTS `Attendances` (
    `Id`             INT           NOT NULL AUTO_INCREMENT,
    `EmployeeId`     INT           NOT NULL,
    `AttendanceDate` DATE          NOT NULL,
    `CheckInTime`    DATETIME(6)   NULL,
    `CheckOutTime`   DATETIME(6)   NULL,
    `WorkHours`      DECIMAL(5,2)  NULL,
    `Status`         VARCHAR(20)   NOT NULL DEFAULT 'Present',
    `IsCompleted`    TINYINT(1)    NOT NULL DEFAULT 0,
    CONSTRAINT `PK_Attendances` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Attendances_Employees_EmployeeId`
        FOREIGN KEY (`EmployeeId`) REFERENCES `Employees`(`Id`)
        ON DELETE CASCADE
);
CREATE UNIQUE INDEX `UX_Attendance_Employee_Date`
    ON `Attendances` (`EmployeeId`, `AttendanceDate`);

-- AttendanceLogs table (every check-in and check-out event)
CREATE TABLE IF NOT EXISTS `AttendanceLogs` (
    `Id`           INT           NOT NULL AUTO_INCREMENT,
    `AttendanceId` INT           NOT NULL,
    `EmployeeId`   INT           NOT NULL,
    `EmployeeName` VARCHAR(100)  NOT NULL,
    `CheckInTime`  DATETIME(6)   NULL,
    `CheckOutTime` DATETIME(6)   NULL,
    `LogDate`      DATE          NOT NULL,
    `CreatedAt`    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT `PK_AttendanceLogs` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AttendanceLogs_Employees`
        FOREIGN KEY (`EmployeeId`) REFERENCES `Employees`(`Id`)
        ON DELETE CASCADE,
    CONSTRAINT `FK_AttendanceLogs_Attendances`
        FOREIGN KEY (`AttendanceId`) REFERENCES `Attendances`(`Id`)
        ON DELETE CASCADE
);
CREATE INDEX `IX_AttendanceLogs_EmployeeId` ON `AttendanceLogs` (`EmployeeId`);
CREATE INDEX `IX_AttendanceLogs_LogDate` ON `AttendanceLogs` (`LogDate`);

-- MonthlyAttendanceSummary table
CREATE TABLE IF NOT EXISTS `MonthlyAttendanceSummary` (
    `Id`             INT           NOT NULL AUTO_INCREMENT,
    `EmployeeId`     INT           NOT NULL,
    `Month`          INT           NOT NULL,
    `Year`           INT           NOT NULL,
    `TotalDays`      INT           NOT NULL DEFAULT 0,
    `IsReset`        TINYINT(1)    NOT NULL DEFAULT 0,
    `ResetAt`        DATETIME      NULL,
    `ResetByManager` VARCHAR(100)  NULL,
    CONSTRAINT `PK_MonthlyAttendanceSummary` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Monthly_Employees`
        FOREIGN KEY (`EmployeeId`) REFERENCES `Employees`(`Id`)
        ON DELETE CASCADE
);
CREATE UNIQUE INDEX `UX_Monthly_Employee_Month_Year`
    ON `MonthlyAttendanceSummary` (`EmployeeId`, `Month`, `Year`);

-- EF Core migrations history
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId`    VARCHAR(150) NOT NULL,
    `ProductVersion` VARCHAR(32)  NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
);
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250101000000_InitialCreate', '8.0.0');

-- Seed demo employee
INSERT INTO `Employees` (`Id`, `EmployeeCode`, `Name`, `Email`, `SupervisorId`, `IsActive`)
VALUES (1, 'EMP001', 'Demo Employee', 'demo@spl.com', NULL, 1);
```

### Tables Overview

| Table | Purpose |
|---|---|
| `Employees` | Employee records with self-referencing supervisor FK |
| `Attendances` | One summary row per employee per day — total work hours, completion status |
| `AttendanceLogs` | Every individual check-in and check-out event |
| `MonthlyAttendanceSummary` | Monthly completed day count per employee with manager reset support |
| `__EFMigrationsHistory` | EF Core internal migration tracking |

> `SupervisorId` is a **self-referencing FK** — supervisors are employees in the same table. No separate supervisor table is needed.

> `IsCompleted = 1` on an Attendance row means the employee completed a full check-in + check-out cycle that day. This is what gets counted toward monthly totals.

---

## Running the API

```bash
cd SPL.Attendance.API
dotnet run
```

Swagger UI opens at the root URL (e.g. `https://localhost:7001/`).

---

## API Endpoints

### Employee Management

| Method | Endpoint | Body | Description |
|---|---|---|---|
| GET | `/api/employees` | — | List all active employees |
| GET | `/api/employees/{id}` | — | Get one employee by ID |
| POST | `/api/employees` | JSON | Create a new employee |
| PUT | `/api/employees/{id}` | JSON | Update name / email / supervisor |
| DELETE | `/api/employees/{id}` | — | Soft deactivate (records preserved) |

**Create Employee — request body:**
```json
{
  "employeeCode": "EMP002",
  "name": "Rahim Uddin",
  "email": "rahim@spl.com",
  "supervisorId": null
}
```

---

### Attendance

| Method | Endpoint | Body | Description |
|---|---|---|---|
| POST | `/api/attendance/checkin` | `{ "employeeId": 1 }` | Record a check-in (multiple per day allowed) |
| POST | `/api/attendance/checkout` | `{ "employeeId": 1 }` | Close latest open check-in, calculate work hours |
| GET | `/api/attendance/{employeeId}` | — | Full attendance history (summary rows) |
| GET | `/api/attendance/{employeeId}/{date}` | — | Attendance summary for a specific date |

---

### Attendance Logs

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/attendance/{employeeId}/logs` | All check-in / check-out log events for an employee |
| GET | `/api/attendance/{employeeId}/logs/{date}` | Log events for a specific date (yyyy-MM-dd) |

---

### Monthly Attendance

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/attendance/{employeeId}/monthly?month=3&year=2026` | Get total completed days for a month |
| POST | `/api/attendance/{employeeId}/reset?month=3&year=2026&managerName=Admin` | Manager resets monthly count to zero |

---

### Response Envelope

All responses use a consistent structure:

```json
{
  "success": true,
  "message": "Employee 1 checked in successfully at 09:00:00.",
  "data": null
}
```

---

### Business Rules Enforced

| Rule | HTTP Response |
|---|---|
| Multiple check-ins per day | ✅ Allowed — each creates a new log entry |
| Check-out without any check-in today | `400 Bad Request` |
| Check-out with no open log (already checked out) | `400 Bad Request` |
| Duplicate EmployeeCode on create | `400 Bad Request` |
| Employee cannot be their own supervisor | `400 Bad Request` |
| Unknown or inactive employee | `404 Not Found` |

---

### How Multiple Check-Ins Work

```
09:00 → Check-In   → Log row 1 created  (CheckIn=09:00, CheckOut=null)
13:00 → Check-Out  → Log row 1 updated  (CheckIn=09:00, CheckOut=13:00)
14:00 → Check-In   → Log row 2 created  (CheckIn=14:00, CheckOut=null)
17:30 → Check-Out  → Log row 2 updated  (CheckIn=14:00, CheckOut=17:30)

Total WorkHours = 4.00 + 3.50 = 7.50 hours
IsCompleted     = true → counts as 1 completed day in monthly summary
```

---

## Running Tests

```bash
cd SPL.Attendance.Tests
dotnet test --verbosity normal
```

### Test Cases

| Test | Scenario | Expected |
|---|---|---|
| TC-001 | First check-in of day | ✅ Record created, log entry added |
| TC-002 | Multiple check-ins same day | ✅ Each creates a new log entry |
| TC-003 | Check-out after valid check-in | ✅ Log updated, WorkHours calculated |
| TC-004 | Check-out without any check-in | ✅ `InvalidOperationException` thrown |
| TC-005 | Check-out with no open log | ✅ `InvalidOperationException` thrown |
| TC-006 | Work hours accuracy (09:00–17:30) | ✅ `WorkHours = 8.50` |
| TC-007 | Create employee — duplicate code | ✅ `InvalidOperationException` thrown |
| TC-008 | Create employee — invalid supervisor | ✅ `KeyNotFoundException` thrown |
| TC-009 | Update employee — self as supervisor | ✅ `InvalidOperationException` thrown |
| TC-010 | Deactivate employee — not found | ✅ `KeyNotFoundException` thrown |

---

## Git Branching Strategy

| Branch | Purpose | Rule |
|---|---|---|
| `main` | Production-ready code | Merged from `develop` via PR after full review |
| `develop` | Integration branch | All features merge here first; always buildable |
| `feature/sprint1-*` | Sprint 1 features | Branch from `develop`, merge back via PR |
| `hotfix/*` | Urgent production fixes | Branch from `main`, merge to both `main` and `develop` |

---

## Commit Message Convention

| Tag | Example |
|---|---|
| `[FEAT]` | `[FEAT] Add Check-In API endpoint with business validation` |
| `[FEAT]` | `[FEAT] Allow multiple check-ins per day with AttendanceLog table` |
| `[FEAT]` | `[FEAT] Add MonthlyAttendanceSummary with manager reset endpoint` |
| `[FEAT]` | `[FEAT] Add Employee CRUD with supervisor self-reference support` |
| `[FIX]` | `[FIX] Add IsCompleted column to Attendances table` |
| `[FIX]` | `[FIX] Upgrade all projects from net8.0 to net9.0` |
| `[REFACTOR]` | `[REFACTOR] Move connection string to appsettings.json` |
| `[TEST]` | `[TEST] Add unit tests for AttendanceService and EmployeeService` |
| `[DOCS]` | `[DOCS] Update README with all Sprint 1 endpoints and tables` |
| `[DB]` | `[DB] Add AttendanceLogs and MonthlyAttendanceSummary tables` |

---

## Sprint Roadmap

| Sprint | Goal | Key Features | Status |
|---|---|---|---|
| Sprint 1 | Core Attendance Logic | Check-In / Check-Out / Multiple check-ins / Work Hours / Employee CRUD / Attendance Logs / Monthly Count / Manager Reset | ✅ Done |
| Sprint 2 | Office Hour Rules | Late detection, Early leave, `OfficeSettings` configuration | 🔜 Upcoming |
| Sprint 3 | Supervisor Approval | Approval workflow, Rejection flow, Status tracking | 🔜 Upcoming |
| Sprint 4 | Movement Tracking | Employee out/in log, Reason entry, Supervisor visibility | 🔜 Upcoming |
| Sprint 5 | Notifications | Missed check-in alerts, Emergency approval notifications | 🔜 Upcoming |

---

*SPL Attendance Management System · Sprint 1 Technical Blueprint · Version 1.1*
*Document prepared for SPL Development Team*
