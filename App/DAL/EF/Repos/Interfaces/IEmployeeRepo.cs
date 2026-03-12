using DAL.EF.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.EF.Repos.Interfaces
{
    public interface IEmployeeRepo
    {
        Task<List<Employee>> GetAll();
        Task<Employee> GetById(int EmployeeId);
        Task Add(Employee employee);
        Task Update(Employee employee);
        Task Delete(int id);
    }
    
}
