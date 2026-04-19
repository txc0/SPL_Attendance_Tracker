using Microsoft.EntityFrameworkCore;
using SPL.Attendance.Data.Context;
using SPL.Attendance.Data.Entities;

namespace SPL.Attendance.Data.Repositories
{
    public class CompanyPolicyRepository : ICompanyPolicyRepository
    {
        private readonly SPLAttendanceDbContext _context;

        public CompanyPolicyRepository(SPLAttendanceDbContext context)
        {
            _context = context;
        }

        public async Task<CompanyPolicy?> GetActiveAsync()
        {
            return await _context.CompanyPolicies
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x => x.IsActive);
        }
    }
}