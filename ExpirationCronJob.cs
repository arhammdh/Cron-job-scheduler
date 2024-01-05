using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Web.Api.Services
{
    public class ExpirationCronJob : CronJobService
    {
        private readonly ILogger<ExpirationCronJob> _logger;
        private readonly IServiceScopeFactory scopeFactory;
        public ExpirationCronJob(
            IScheduleConfig<ExpirationCronJob> config,
            ILogger<ExpirationCronJob> logger,
            IServiceScopeFactory scopeFactory
            )
            : base(config.CronExpression, config.TimeZoneInfo, scopeFactory)
        {
            _logger = logger;
            this.scopeFactory = scopeFactory;

        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }
        public override async Task DoWork(CancellationToken cancellationToken)
        {
            //Add Logic Here
        }
        public override async Task SendNotification(CancellationToken cancellationToken, int notificationId, int countryId)
        {
            //implement Notification send logic
            CancelScheduledJob(notificationId);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }
    }
}
