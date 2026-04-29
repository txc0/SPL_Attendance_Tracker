using System.Threading;
using System.Threading.Tasks;

namespace SPL.Attendance.Data.Repositories
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}     