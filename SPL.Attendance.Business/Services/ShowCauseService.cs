using SPL.Attendance.Business.Interfaces;
using SPL.Attendance.Business.Models;
using SPL.Attendance.Data.Entities;
using SPL.Attendance.Data.Repositories;

namespace SPL.Attendance.Business.Services
{
    public class ShowCauseService : IShowCauseService
    {
        private readonly IShowCauseRepository _showCauseRepo;
        private readonly IEmployeeRepository _employeeRepo;

        public ShowCauseService(IShowCauseRepository showCauseRepo,
                                IEmployeeRepository employeeRepo)
        {
            _showCauseRepo = showCauseRepo;
            _employeeRepo = employeeRepo;
        }

        public async Task<ShowCauseRequestDto> SubmitAsync(int employeeId, string reason, string type = "LOGIN")
        {
            var employee = await _employeeRepo.GetByIdAsync(employeeId)
                ?? throw new KeyNotFoundException(
                    $"Employee {employeeId} not found.");

            if (employee.SupervisorId == null)
                throw new InvalidOperationException(
                    "You have no supervisor assigned. " +
                    "Contact HR to assign a supervisor.");

            // Check no pending request already exists
            var existing = await _showCauseRepo
                .GetPendingByEmployeeAsync(employeeId);

            if (existing != null)
                throw new InvalidOperationException(
                    "You already have a pending show cause request. " +
                    "Please wait for your supervisor to review it.");

            var request = new ShowCauseRequest
            {
                EmployeeId = employeeId,
                SupervisorId = employee.SupervisorId.Value,
                Reason = reason.Trim(),
                Status = "Pending",
                RequestedAt = DateTime.Now,
                Type = type
            };

            var created = await _showCauseRepo.AddAsync(request);
            return MapToDto(created, employee.Name,
                            employee.Supervisor?.Name ?? "Supervisor");
        }

        public async Task ReviewAsync(int showCauseId, int supervisorId,
                                      bool isApproved, string? reviewNote)
        {
            var request = await _showCauseRepo.GetByIdAsync(showCauseId)
                ?? throw new KeyNotFoundException(
                    $"Show cause request {showCauseId} not found.");

            if (request.SupervisorId != supervisorId)
                throw new UnauthorizedAccessException(
                    "You are not the assigned supervisor for this request.");

            if (request.Status != "Pending")
                throw new InvalidOperationException(
                    "This request has already been reviewed.");

            request.Status = isApproved ? "Approved" : "Rejected";
            request.ReviewedAt = DateTime.Now;
            request.ReviewNote = reviewNote;

            await _showCauseRepo.UpdateAsync(request);
        }

        public async Task<List<ShowCauseRequestDto>> GetPendingForSupervisorAsync(
            int supervisorId)
        {
            var requests = await _showCauseRepo
                .GetPendingBySupervisorAsync(supervisorId);

            return requests.Select(r => MapToDto(
                r,
                r.Employee?.Name ?? string.Empty,
                r.Supervisor?.Name ?? string.Empty))
                .ToList();
        }

        public async Task<ShowCauseRequestDto?> GetPendingForEmployeeAsync(
            int employeeId)
        {
            var request = await _showCauseRepo
                .GetPendingByEmployeeAsync(employeeId);

            if (request == null) return null;

            return MapToDto(
                request,
                request.Employee?.Name ?? string.Empty,
                request.Supervisor?.Name ?? string.Empty);
        }

        private static ShowCauseRequestDto MapToDto(
            ShowCauseRequest r,
            string employeeName,
            string supervisorName) => new()
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                EmployeeName = employeeName,
                SupervisorId = r.SupervisorId,
                Reason = r.Reason,
                Status = r.Status,
                RequestedAt = r.RequestedAt,
                ReviewedAt = r.ReviewedAt,
                ReviewNote = r.ReviewNote
            };

        public async Task<ShowCauseRequestDto> SubmitByEmailAsync(string email, string reason, string type)
        {
            var employee = await _employeeRepo.GetByEmailAsync(email)
                ?? throw new KeyNotFoundException(
                    $"No employee found with email '{email}'.");

            return await SubmitAsync(employee.Id, reason, type);
        }


    }
}