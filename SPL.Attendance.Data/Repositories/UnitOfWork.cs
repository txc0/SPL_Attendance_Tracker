using System.Threading;
using System.Threading.Tasks;
using SPL.Attendance.Data.Context;

namespace SPL.Attendance.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SPLAttendanceDbContext _context;

        public UnitOfWork(SPLAttendanceDbContext context)
        {
            _context = context;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}