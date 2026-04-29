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
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x => x.IsActive);
        }

        public async Task<CompanyPolicy> UpsertActiveAsync(CompanyPolicy policy)
        {
            var current = await _context.CompanyPolicies
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x => x.IsActive);

            if (current == null)
            {
                policy.IsActive = true;
                policy.CreatedAt = DateTime.Now;
                _context.CompanyPolicies.Add(policy);
                return policy;
            }

            current.WorkStartTime = policy.WorkStartTime;
            current.WorkEndTime = policy.WorkEndTime;
            current.RequireApprovalForMultipleLogin = policy.RequireApprovalForMultipleLogin;
            current.AutoLogoutAfterShift = policy.AutoLogoutAfterShift;
            current.UpdatedAt = DateTime.Now;

            return current;
        }
    }
}