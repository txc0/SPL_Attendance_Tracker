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
        private readonly IUnitOfWork _unitOfWork;

        public EmployeeService(IEmployeeRepository employeeRepo, IUnitOfWork unitOfWork)
        {
            _employeeRepo = employeeRepo;
            _unitOfWork = unitOfWork;
        }

        // ── GET ALL 

        public async Task<List<EmployeeDto>> GetAllEmployeesAsync()
        {
            var employees = await _employeeRepo.GetAllAsync();
            return employees.Select(MapToDto).ToList();
        }

        // ── GET BY ID 

        public async Task<EmployeeDto> GetEmployeeByIdAsync(int id)
        {
            var employee = await _employeeRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Employee with ID {id} was not found.");

            return MapToDto(employee);
        }

        // ── CREATE 

        public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto)
        {
            // Business Rule: EmployeeCode must be unique across the system
            if (await _employeeRepo.CodeExistsAsync(dto.EmployeeCode))
                throw new InvalidOperationException(
                    $"Employee code '{dto.EmployeeCode}' is already in use. " +
                    "Each employee must have a unique employee code.");

            // Business Rule: If a supervisor is assigned, they must exist and not create a cycle
            if (dto.SupervisorId.HasValue)
                await ValidateSupervisorAssignmentAsync(null, dto.SupervisorId.Value);

            var employee = new Employee
            {
                EmployeeCode = dto.EmployeeCode.Trim().ToUpper(),
                Name         = dto.Name.Trim(),
                Email        = dto.Email?.Trim().ToLower(),
                SupervisorId = dto.SupervisorId,
                IsSupervisor = dto.IsSupervisor,
                IsActive     = true
            };

            var created = await _employeeRepo.AddAsync(employee);
            await _unitOfWork.SaveChangesAsync();

            var full = await _employeeRepo.GetByIdAsync(created.Id);
            return MapToDto(full!);
        }

        // ── UPDATE 

        public async Task<EmployeeDto> UpdateEmployeeAsync(int id, UpdateEmployeeDto dto)
        {
            var employee = await _employeeRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Employee with ID {id} was not found.");

            // Business Rule: New supervisor must exist (if provided) and must not create a cycle
            if (dto.SupervisorId.HasValue)
                await ValidateSupervisorAssignmentAsync(id, dto.SupervisorId.Value);

            employee.Name        = dto.Name.Trim();
            employee.Email       = dto.Email?.Trim().ToLower();
            employee.IsSupervisor = dto.IsSupervisor;
            employee.SupervisorId = dto.SupervisorId;

            await _employeeRepo.UpdateAsync(employee);
            await _unitOfWork.SaveChangesAsync();

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
            await _unitOfWork.SaveChangesAsync();
        }

        // ── MAPPER ──────────────────────────────────────────────────────────

        private static EmployeeDto MapToDto(Employee e) => new()
        {
            Id             = e.Id,
            EmployeeCode   = e.EmployeeCode,
            Name           = e.Name,
            Email          = e.Email,
            SupervisorId   = e.SupervisorId,
            SupervisorName = e.Supervisor?.Name ?? string.Empty,
            IsActive       = e.IsActive
        };

        private async Task ValidateSupervisorAssignmentAsync(int? employeeId, int supervisorId)
        {
            var visited = new HashSet<int>();
            if (employeeId.HasValue)
                visited.Add(employeeId.Value);

            var currentId = supervisorId;

            while (true)
            {
                if (!visited.Add(currentId))
                    throw new InvalidOperationException(
                        "Supervisor assignment creates a circular reference.");

                var supervisor = await _employeeRepo.GetByIdAsync(currentId);
                if (supervisor == null || !supervisor.IsActive)
                    throw new KeyNotFoundException(
                        $"Supervisor with ID {currentId} was not found.");

                if (!supervisor.SupervisorId.HasValue)
                    break;

                currentId = supervisor.SupervisorId.Value;
            }
        }
    }
}
