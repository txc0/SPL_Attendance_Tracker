using SPL.Attendance.Business.Interfaces;
using SPL.Attendance.Business.Models;
using SPL.Attendance.Data.Entities;
using SPL.Attendance.Data.Repositories;

namespace SPL.Attendance.Business.Services
{
    /// <summary>
    /// Implements all employee management business rules.
    /// </summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepo;

        public EmployeeService(IEmployeeRepository employeeRepo)
        {
            _employeeRepo = employeeRepo;
        }

        // ── GET ALL ─────────────────────────────────────────────────────────

        public async Task<List<EmployeeDto>> GetAllEmployeesAsync()
        {
            var employees = await _employeeRepo.GetAllAsync();
            return employees.Select(MapToDto).ToList();
        }

        // ── GET BY ID ───────────────────────────────────────────────────────

        public async Task<EmployeeDto> GetEmployeeByIdAsync(int id)
        {
            var employee = await _employeeRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Employee with ID {id} was not found.");

            return MapToDto(employee);
        }

        // ── CREATE ──────────────────────────────────────────────────────────

        public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto)
        {
            // Business Rule: EmployeeCode must be unique across the system
            if (await _employeeRepo.CodeExistsAsync(dto.EmployeeCode))
                throw new InvalidOperationException(
                    $"Employee code '{dto.EmployeeCode}' is already in use. " +
                    "Each employee must have a unique employee code.");

            // Business Rule: If a supervisor is assigned, they must exist
            if (dto.SupervisorId.HasValue)
            {
                if (!await _employeeRepo.ExistsAsync(dto.SupervisorId.Value))
                    throw new KeyNotFoundException(
                        $"Supervisor with ID {dto.SupervisorId.Value} was not found. " +
                        "Please assign a valid supervisor.");
            }

            var employee = new Employee
            {
                EmployeeCode = dto.EmployeeCode.Trim().ToUpper(),
                Name         = dto.Name.Trim(),
                Email        = dto.Email?.Trim().ToLower(),
                SupervisorId = dto.SupervisorId,
                IsActive     = true
            };

            var created = await _employeeRepo.AddAsync(employee);

            // Re-fetch to include supervisor name
            var full = await _employeeRepo.GetByIdAsync(created.Id);
            return MapToDto(full!);
        }

        // ── UPDATE ──────────────────────────────────────────────────────────

        public async Task<EmployeeDto> UpdateEmployeeAsync(int id, UpdateEmployeeDto dto)
        {
            var employee = await _employeeRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Employee with ID {id} was not found.");

            // Business Rule: New supervisor must exist (if provided)
            if (dto.SupervisorId.HasValue)
            {
                // Prevent self-assignment as supervisor
                if (dto.SupervisorId.Value == id)
                    throw new InvalidOperationException(
                        "An employee cannot be their own supervisor.");

                if (!await _employeeRepo.ExistsAsync(dto.SupervisorId.Value))
                    throw new KeyNotFoundException(
                        $"Supervisor with ID {dto.SupervisorId.Value} was not found.");
            }

            employee.Name        = dto.Name.Trim();
            employee.Email       = dto.Email?.Trim().ToLower();
            employee.SupervisorId = dto.SupervisorId;

            await _employeeRepo.UpdateAsync(employee);

            var updated = await _employeeRepo.GetByIdAsync(id);
            return MapToDto(updated!);
        }

        // ── DEACTIVATE (soft delete) ─────────────────────────────────────────

        public async Task DeactivateEmployeeAsync(int id)
        {
            var exists = await _employeeRepo.ExistsAsync(id);
            if (!exists)
                throw new KeyNotFoundException($"Employee with ID {id} was not found.");

            await _employeeRepo.DeactivateAsync(id);
        }

        // ── MAPPER ──────────────────────────────────────────────────────────

        private static EmployeeDto MapToDto(Employee e) => new()
        {
            Id             = e.Id,
            EmployeeCode   = e.EmployeeCode,
            Name           = e.Name,
            Email          = e.Email,
            SupervisorId   = e.SupervisorId,
            SupervisorName = e.Supervisor?.Name,
            IsActive       = e.IsActive
        };
    }
}
