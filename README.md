# SPL Attendance Management System

> ASP.NET Core 8 Web API · Entity Framework Core 8 · MySQL · React 18 · JWT

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Technology Stack](#technology-stack)
3. [Architecture](#architecture)
4. [Project Structure](#project-structure)
5. [Getting Started](#getting-started)
6. [Database Setup](#database-setup)
7. [Running the API](#running-the-api)
8. [Running the Frontend](#running-the-frontend)
9. [API Endpoints](#api-endpoints)
10. [Running Tests](#running-tests)
11. [Git Branching Strategy](#git-branching-strategy)
12. [Commit Message Convention](#commit-message-convention)

---

## Project Overview

The **SPL Attendance Management System** is a full-stack attendance tracker with an ASP.NET Core API and a React client. The backend implements attendance rules, login/logout tracking, and supervisor approvals for repeated sign-ins.

Key capabilities:
- **Employee management** with supervisor relationships and activation status
- **Attendance check-in/out** with per-day summaries and detailed log entries
- **Multiple sign-ins** handled via **show cause approval** workflow
- **JWT authentication** for login/logout and supervisor actions
- **React dashboard** for end users (login + attendance view)
- **Postman collection** for API testing (`SPL_Attendance_Sprint1.postman_collection.json`)

---

## Technology Stack

| Layer | Technology |
|---|---|
| Backend Framework | ASP.NET Core 8 (Web API) |
| ORM | Entity Framework Core 8 (Code-First) |
| Database | MySQL 8.x (Pomelo EF Core provider) |
| Authentication | JWT (Bearer) + BCrypt password hashing |
| API Documentation | Swagger / Swashbuckle |
| Frontend | React 18 + Axios |
| Unit Testing | xUnit + Moq + FluentAssertions |
| Version Control | Git + GitHub |
| IDE | Visual Studio 2022 / VS Code |

---

## Architecture

The solution follows a **3-tier API architecture** with a separate React client.

```
┌──────────────────────────────────────┐
│   React Frontend (spl-attendance-    │
│   client)                            │
└──────────────────┬───────────────────┘
                   │ HTTPS
┌──────────────────▼───────────────────┐
│  APPLICATION LAYER                   │
│  SPL.Attendance.API                  │
│  Controllers: Auth / Attendance /    │
│  Employee / ShowCause                │
└──────────────────┬───────────────────┘
                   │ Services (Business rules)
┌──────────────────▼───────────────────┐
│  BUSINESS LOGIC LAYER                │
│  SPL.Attendance.Business             │
│  AttendanceService / AuthService     │
│  ShowCauseService / EmployeeService  │
└──────────────────┬───────────────────┘
                   │ Repositories
┌──────────────────▼───────────────────┐
│  DATA ACCESS LAYER                   │
│  SPL.Attendance.Data (EF Core)       │
└──────────────────┬───────────────────┘
                   │
┌──────────────────▼───────────────────┐
│  MySQL Database                       │
└──────────────────────────────────────┘
```

---

## Project Structure

```
SPL.AttendanceManagementSystem.sln
│
├── SPL.Attendance.API/                        ← Application Layer (Web API)
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── AttendanceController.cs
│   │   ├── EmployeeController.cs
│   │   └── ShowCauseController.cs
│   ├── DTOs/
│   │   ├── AttendanceRequests.cs
│   │   ├── AuthDtos.cs
│   │   ├── EmployeeRequests.cs
│   │   ├── ShowCauseRequests.cs
│   │   └── ApiResponse.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Program.cs                              ← DI + Swagger + CORS + EF setup
│   └── appsettings.Example.json                ← Template config (copy to appsettings.json)
│
├── SPL.Attendance.Business/                   ← Business Logic Layer
│   ├── Interfaces/
│   │   ├── IAttendanceService.cs
│   │   ├── IAuthService.cs
│   │   ├── IEmployeeService.cs
│   │   └── IShowCauseService.cs
│   ├── Models/
│   │   ├── AttendanceRecordDto.cs
│   │   ├── AttendanceLogDto.cs
│   │   ├── EmployeeDtos.cs
│   │   ├── LoginResultDto.cs
│   │   └── ShowCauseDto.cs
│   └── Services/
│       ├── AttendanceService.cs
│       ├── AuthService.cs
│       ├── EmployeeService.cs
│       └── ShowCauseService.cs
│
├── SPL.Attendance.Data/                       ← Data Access Layer
│   ├── Entities/
│   │   ├── Attendance.cs
│   │   ├── AttendanceLog.cs
│   │   ├── Employee.cs
│   │   ├── MonthlyAttendanceSummary.cs
│   │   └── ShowCauseRequest.cs
│   ├── Entities/Context/
│   │   └── SPLAttendanceDbContext.cs
│   ├── Repositories/
│   │   ├── AttendanceRepository.cs
│   │   ├── EmployeeRepository.cs
│   │   └── ShowCauseRepository.cs
│   └── Migrations/
│
├── SPL.Attendance.Tests/                      ← xUnit Unit Tests
│   ├── AttendanceServiceTests.cs
│   └── EmployeeServiceTests.cs
│
├── spl-attendance-client/                     ← React client
│   ├── public/
│   └── src/
│
├── SPL_Attendance_Sprint1.postman_collection.json
├── global.json                                ← Pins .NET SDK to 8.0.x
└── README.md
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (see `global.json`)
- [Node.js 18+](https://nodejs.org/)
- [MySQL 8.x](https://dev.mysql.com/downloads/mysql/)
- Git

### 1. Clone the repository

```bash
git clone https://github.com/txc0/SPL_Attendance_Tracker.git
cd SPL_Attendance_Tracker
```

### 2. Configure appsettings.json

Copy the template and add your MySQL + JWT settings:

```bash
cp SPL.Attendance.API/appsettings.Example.json SPL.Attendance.API/appsettings.json
```

```json
{
  "ConnectionStrings": {
    "SPLAttendanceDB": "server=localhost;port=3306;database=SPLAttendanceDB;uid=root;password=YOUR_PASSWORD;"
  },
  "Jwt": {
    "Key": "REPLACE_WITH_A_LONG_RANDOM_SECRET",
    "Issuer": "spl-attendance-api",
    "Audience": "spl-attendance-client",
    "ExpiryHours": "8"
  }
}
```

> `appsettings.json` is ignored by Git. Keep secrets out of source control.

---

## Database Setup

- Create an empty MySQL database (e.g., `SPLAttendanceDB`) and keep the name consistent with `appsettings.json`.
- The API applies EF Core migrations automatically on startup (`db.Database.Migrate()`).
- If you prefer manual migration: install the EF tool (`dotnet tool install --global dotnet-ef`) and run `dotnet ef database update --project SPL.Attendance.API`.

### Tables Overview

| Table | Purpose |
|---|---|
| `Employees` | Employee records, supervisor relationship, login credentials |
| `Attendances` | One summary row per employee per day (check-in/out, totals) |
| `AttendanceLogs` | Every individual check-in/check-out event |
| `MonthlyAttendanceSummary` | Per-month completed day counts |
| `ShowCauseRequests` | Approval workflow for repeated login/logout |

---

## Running the API

```bash
dotnet run --project SPL.Attendance.API
```

Swagger UI opens at the root URL in Development (e.g., `https://localhost:7001/`).

---

## Running the Frontend

```bash
cd spl-attendance-client
npm install
npm start
```

The React app proxies API requests to `https://localhost:7001` (see `package.json`).

---

## API Endpoints

### Auth

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/api/auth/login` | Login with email + password | — |
| GET | `/api/auth/needs-logout-approval/{employeeId}` | Check if logout needs approval | Bearer |
| POST | `/api/auth/logout/{employeeId}` | Record logout | Bearer |
| POST | `/api/auth/set-password` | Set employee password (current API uses query params; recommended to move to JSON body) | Admin |

> Security warning: the current API expects query parameters for `set-password`, which is insecure because credentials can be logged. Treat this as a vulnerability and **fix before production use** by switching the API to a JSON body (e.g., `{ "employeeId": 1, "password": "..." }`).

### Employees

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/employees` | List all active employees |
| GET | `/api/employees/{id}` | Get one employee by ID |
| POST | `/api/employees` | Create a new employee |
| PUT | `/api/employees/{id}` | Update employee |
| DELETE | `/api/employees/{id}` | Soft deactivate |

### Attendance

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/attendance/checkin` | Record a check-in |
| POST | `/api/attendance/checkout` | Record a check-out |
| GET | `/api/attendance/{employeeId}` | Attendance history |
| GET | `/api/attendance/{employeeId}/{date}` | Attendance summary for date (yyyy-MM-dd) |
| GET | `/api/attendance/all?filter=today|week|month` | Aggregated attendance list |

### Attendance Logs

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/attendance/{employeeId}/logs` | All check-in/out log events |
| GET | `/api/attendance/{employeeId}/logs/{date}` | Logs for a specific date |

### Show Cause Approval

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/api/showcause/submit?employeeId=1&reason=...&type=LOGIN` | Submit show cause | — |
| POST | `/api/showcause/submitbyemail?email=...&reason=...&type=LOGIN` | Submit show cause by email | — |
| POST | `/api/showcause/review/{requestId}` | Review request (body: Approved/Rejected) | Admin |
| POST | `/api/showcause/review?showCauseId=...&supervisorId=...&isApproved=true` | Legacy review endpoint | Admin |
| GET | `/api/showcause/pending` | Pending requests for supervisor | Admin |
| GET | `/api/showcause/pending/{supervisorId}` | Pending requests by supervisor ID | — |
| GET | `/api/showcause/employee/{employeeId}` | Pending request for employee | — |

> Protected endpoints require `Authorization: Bearer {token}` from `/api/auth/login`.

---

## Running Tests

```bash
dotnet test SPL.AttendanceManagementSystem.sln --verbosity normal
```

---

## Git Branching Strategy

| Branch | Purpose | Rule |
|---|---|---|
| `main` | Production-ready code | Merged via PR after review |
| `develop` | Integration branch | All features merge here first |
| `feature/*` | Feature branches | Branch from `develop` |
| `hotfix/*` | Urgent fixes | Branch from `main`, merge to `main` + `develop` |

---

## Commit Message Convention

| Tag | Example |
|---|---|
| `[FEAT]` | `[FEAT] Add Check-In API endpoint with business validation` |
| `[FIX]` | `[FIX] Correct attendance work hour calculation` |
| `[REFACTOR]` | `[REFACTOR] Extract auth service helpers` |
| `[TEST]` | `[TEST] Add unit tests for AttendanceService` |
| `[DOCS]` | `[DOCS] Update README with current endpoints` |
| `[DB]` | `[DB] Update attendance schema` |

---

*SPL Attendance Management System · Updated README*
