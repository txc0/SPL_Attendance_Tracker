using SPL.Attendance.Data.Entities;

namespace SPL.Attendance.Data.Repositories
{
    public interface ICompanyPolicyRepository
    {
        Task<CompanyPolicy?> GetActiveAsync();
        Task<CompanyPolicy> UpsertActiveAsync(CompanyPolicy policy);
    }
}