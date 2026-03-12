using DAL.EF.Models;
using DAL.EF.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using DAL.EF;
using static DAL.EF.Repos.Implementations.EmployeeRepo;

namespace DAL.EF.Repos.Implementations
{
    public class EmployeeRepo : IEmployeeRepo
    {
        private readonly SPL_Context _context;

        public EmployeeRepo(SPL_Context context)
        {
            _context = context;
        }

        public async Task<List<Employee>> GetAll()
        {
            return await _context.Employees
                .Where(e => e.IsActive)
                .Include(e => e.Supervisor)
                .ToListAsync();
        }

        public async Task<Employee> GetById(int id)
        {
            return await _context.Employees
                .Include(e => e.Supervisor)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Employee> GetByCode(string employeeCode)
        {
            return await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeCode);
        }

        public async Task Add(Employee employee)
        {
            employee.Created_at = TimeOnly.FromDateTime(DateTime.Now);
            employee.IsActive = true;
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Employee employee)
        {
            _context.Entry(employee).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                employee.IsActive = false; // Soft delete
                _context.Entry(employee).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
        }
    }
    
}
