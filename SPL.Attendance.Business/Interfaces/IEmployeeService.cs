using SPL.Attendance.Business.Models;

namespace SPL.Attendance.Business.Interfaces
{
  
    public interface IEmployeeService
    {
        Task<List<EmployeeDto>> GetAllEmployeesAsync();

        Task<EmployeeDto> GetEmployeeByIdAsync(int id);

        Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto);

        Task<EmployeeDto> UpdateEmployeeAsync(int id, UpdateEmployeeDto dto);

        Task DeactivateEmployeeAsync(int id);
    }
}
