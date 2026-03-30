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
11. [Sprint Roadmap](#sprint-roadmap)

---

## Project Overview

The **SPL Attendance Management System** is an enterprise-grade web application for managing employee attendance, movement tracking, and supervisor approvals within SPL.

**Sprint 1** delivers the core attendance lifecycle:
- Employee **Check-In** (one per calendar day, duplicate-guarded)
- Employee **Check-Out** (requires prior check-in, calculates work hours)
- **Attendance History** and **date-wise record retrieval**

---

## Technology Stack

| Layer              | Technology                          |
|--------------------|-------------------------------------|
| Backend Framework  | ASP.NET Core 8 (Web API)            |
| ORM                | Entity Framework Core 8 (Code-First)|
| Database           | MySQL 8.x (Pomelo EF Core provider) |
| Dependency Injection | Built-in ASP.NET Core DI          |
| API Documentation  | Swagger / Swashbuckle               |
| Version Control    | Git + GitHub (Git Flow)             |

---

## Architecture

The system follows a strict **3-Tier Architecture**. No layer ever bypasses another.

```
┌──────────────────────────────────┐
│   React Frontend (Sprint 3+)     │
│   POST /api/attendance/checkin   │
└─────────────────┬────────────────┘
                  │ HTTP
┌─────────────────▼────────────────┐
│  APPLICATION LAYER               │
│  SPL.Attendance.API              │
│  AttendanceController.cs         │
│  (Receives HTTP, validates input,│
│   delegates to service — NO BL)  │
└─────────────────┬────────────────┘
                  │ IAttendanceService
┌─────────────────▼────────────────┐
│  BUSINESS LOGIC LAYER            │
│  SPL.Attendance.Business         │
│  AttendanceService.cs            │
│  (All business rules live here)  │
└─────────────────┬────────────────┘
                  │ IAttendanceRepository
┌─────────────────▼────────────────┐
│  DATA ACCESS LAYER               │
│  SPL.Attendance.Data             │
│  AttendanceRepository.cs         │
│  (EF Core + MySQL — NO BL here)  │
└─────────────────┬────────────────┘
                  │
┌─────────────────▼────────────────┐
│  MySQL 8.x Database              │
│  Employees  |  Attendances       │
└──────────────────────────────────┘
```

---

## Project Structure

```
SPL.AttendanceManagementSystem.sln
│
├── SPL.Attendance.API/                  ← Application Layer (Web API)
│   ├── Controllers/
│   │   └── AttendanceController.cs
│   ├── DTOs/
│   │   ├── AttendanceRequests.cs        ← CheckInRequest, CheckOutRequest
│   │   └── ApiResponse.cs              ← Standard response envelope
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Program.cs                       ← DI + Swagger + CORS + EF setup
│   ├── appsettings.json
│   └── appsettings.Example.json         ← Commit this; keep appsettings.json local
│
├── SPL.Attendance.Business/             ← Business Logic Layer
│   ├── Interfaces/
│   │   └── IAttendanceService.cs
│   ├── Models/
│   │   └── AttendanceRecordDto.cs
│   └── Services/
│       └── AttendanceService.cs
│
├── SPL.Attendance.Data/                 ← Data Access Layer
│   ├── Context/
│   │   └── SPLAttendanceDbContext.cs
│   ├── Entities/
│   │   ├── Employee.cs
│   │   └── Attendance.cs
│   ├── Repositories/
│   │   ├── IAttendanceRepository.cs
│   │   └── AttendanceRepository.cs
│   └── Migrations/
│       ├── 20250101000000_InitialCreate.cs
│       └── SPLAttendanceDbContextModelSnapshot.cs
│
├── SPL.Attendance.Tests/                ← xUnit Unit Tests
│   └── AttendanceServiceTests.cs        ← TC-001 through TC-006 + extras
│
├── SPL_Attendance_Sprint1.postman_collection.json
└── .gitignore
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL 8.x](https://dev.mysql.com/downloads/mysql/) running locally on port 3306
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or VS Code with C# Dev Kit

### 1. Clone the repository

```bash
git clone https://github.com/YourOrg/SPL.AttendanceManagementSystem.git
cd SPL.AttendanceManagementSystem
git checkout develop
git checkout -b feature/sprint1-checkin-checkout
```

### 2. Configure the database connection

Copy the example settings and fill in your MySQL password:

```bash
cd SPL.Attendance.API
cp appsettings.Example.json appsettings.json
```

Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "SPLAttendanceDB": "server=localhost;port=3306;database=SPLAttendanceDB;uid=root;password=YOUR_PASSWORD;"
  }
}
```

---

## Database Setup

The application uses **EF Core Code-First migrations**. On first startup, `Program.cs` automatically calls `db.Database.Migrate()` which creates the database and tables.

Alternatively, run migrations manually from the solution root:

```bash
# From the solution root directory
dotnet ef database update \
  --project SPL.Attendance.Data \
  --startup-project SPL.Attendance.API
```

To add a new migration after changing entities:

```bash
dotnet ef migrations add <MigrationName> \
  --project SPL.Attendance.Data \
  --startup-project SPL.Attendance.API
```

### Tables Created

| Table        | Key Columns                                                             |
|--------------|-------------------------------------------------------------------------|
| `Employees`  | `Id`, `EmployeeCode` (unique), `Name`, `Email`, `SupervisorId`, `IsActive` |
| `Attendances`| `Id`, `EmployeeId` (FK), `AttendanceDate`, `CheckInTime`, `CheckOutTime`, `WorkHours`, `Status` |

A unique index `UX_Attendance_Employee_Date` on `(EmployeeId, AttendanceDate)` enforces the one-record-per-day rule at the database level.

---

## Running the API

```bash
cd SPL.Attendance.API
dotnet run
```

Swagger UI is available at the root URL (e.g., `https://localhost:7001/`).

---

## API Endpoints

| Method | Endpoint                                  | Body                      | Description                        |
|--------|-------------------------------------------|---------------------------|------------------------------------|
| POST   | `/api/attendance/checkin`                 | `{ "employeeId": 1 }`     | Record employee check-in           |
| POST   | `/api/attendance/checkout`                | `{ "employeeId": 1 }`     | Check out, calculate work hours    |
| GET    | `/api/attendance/{employeeId}`            | —                         | Full attendance history             |
| GET    | `/api/attendance/{employeeId}/{date}`     | —                         | Record for a specific date (yyyy-MM-dd) |

### Response Envelope

All responses use a consistent envelope:

```json
{
  "success": true,
  "message": "Employee 1 checked in successfully at 09:00:00.",
  "data": null
}
```

### Business Rules Enforced

| Rule                    | HTTP Response          |
|-------------------------|------------------------|
| Duplicate check-in      | `400 Bad Request`      |
| Check-out without check-in | `400 Bad Request`   |
| Duplicate check-out     | `400 Bad Request`      |
| Unknown/inactive employee | `404 Not Found`      |

---

## Running Tests

```bash
cd SPL.Attendance.Tests
dotnet test --verbosity normal
```

### Sprint 1 Test Cases

| Test   | Scenario                            | Expected                              |
|--------|-------------------------------------|---------------------------------------|
| TC-001 | First check-in of day               | ✅ Record created successfully         |
| TC-002 | Duplicate check-in same day         | ✅ `InvalidOperationException` thrown  |
| TC-003 | Check-out after valid check-in      | ✅ `CheckOutTime` saved, `WorkHours` calculated |
| TC-004 | Check-out without check-in          | ✅ `InvalidOperationException` thrown  |
| TC-005 | Duplicate check-out                 | ✅ `InvalidOperationException` thrown  |
| TC-006 | Work hours accuracy (09:00–17:30)   | ✅ `WorkHours = 8.50`                  |

---

## Git Branching Strategy

| Branch             | Purpose                              | Rule                                        |
|--------------------|--------------------------------------|---------------------------------------------|
| `main`             | Production-ready code                | Merged from `develop` via PR after review   |
| `develop`          | Integration branch                   | All features merge here; always buildable   |
| `feature/sprint1-*`| Sprint 1 features                    | Branch from `develop`, merge back via PR    |
| `hotfix/*`         | Urgent production fixes              | Branch from `main`, merge to both           |

### Commit Message Convention

```
[FEAT]     Add Check-In API endpoint with business validation
[FIX]      Correct work hours calculation rounding error
[REFACTOR] Move connection string to appsettings.json
[TEST]     Add unit tests for AttendanceService CheckIn rule
[DOCS]     Update README with API endpoints list
[DB]       Add InitialCreate migration for Employees and Attendances
```

---

## Sprint Roadmap

| Sprint   | Goal                     | Key Features                                              |
|----------|--------------------------|-----------------------------------------------------------|
| ✅ Sprint 1 | Core Attendance Logic  | Check-In / Check-Out / Work Hour Calculation / Date View  |
| Sprint 2 | Office Hour Rules        | Late detection, Early leave, `OfficeSettings` config       |
| Sprint 3 | Supervisor Approval      | Approval workflow, Rejection flow, Status tracking        |
| Sprint 4 | Movement Tracking        | Employee out/in log, Reason entry, Supervisor visibility   |
| Sprint 5 | Notifications            | Missed check-in alerts, Emergency approval notifications   |

---

*SPL Attendance Management System · Sprint 1 Technical Blueprint · Version 1.0*
