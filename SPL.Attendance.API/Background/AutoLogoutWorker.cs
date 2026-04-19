using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SPL.Attendance.Business.Interfaces;
using SPL.Attendance.Data.Repositories;

namespace SPL.Attendance.API.Background
{
    public class AutoLogoutWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoLogoutWorker> _logger;

        public AutoLogoutWorker(IServiceProvider serviceProvider, ILogger<AutoLogoutWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var policyRepo = scope.ServiceProvider.GetRequiredService<ICompanyPolicyRepository>();
                    var attendanceRepo = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
                    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

                    var policy = await policyRepo.GetActiveAsync();
                    if (policy == null || !policy.AutoLogoutAfterShift)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                        continue;
                    }

                    var now = DateTime.Now;
                    if (now.TimeOfDay < policy.WorkEndTime)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                        continue;
                    }

                    var employeeIds = await attendanceRepo.GetEmployeesWithOpenLogsAsync(now.Date);

                    foreach (var employeeId in employeeIds)
                    {
                        await authService.RecordLogoutAsync(employeeId);
                    }

                    if (employeeIds.Count > 0)
                        _logger.LogInformation("Auto-logout completed for {Count} employee(s).", employeeIds.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AutoLogoutWorker failed.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}