using Cronos;
using Infrastructure;
using Infrastructure.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared;

namespace Web.Api.Services
{
    public abstract class CronJobService : IHostedService, IDisposable
    {
        private System.Timers.Timer _timer;
        private readonly CronExpression _expression;
        private readonly TimeZoneInfo _timeZoneInfo;
        private readonly IServiceScopeFactory _scopeFactory;
        public static List<NotoficationSchedule> _jobs;
        protected CronJobService(string cronExpression, TimeZoneInfo timeZoneInfo, IServiceScopeFactory scopeFactory)
        {
            _expression = CronExpression.Parse(cronExpression);
            _timeZoneInfo = timeZoneInfo;
            _scopeFactory = scopeFactory;

        }
        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            _jobs = new List<NotoficationSchedule>();
            await ScheduleJob(cancellationToken);
            await NotificationSchedulePreviousJobs(cancellationToken);
        }
        protected virtual async Task ScheduleJob(CancellationToken cancellationToken)
        {

            try
            {
                var next = _expression.GetNextOccurrence(DateTimeOffset.Now, _timeZoneInfo);
                // DateTimeOffset? next = DateTimeOffset.Now.AddMinutes(1);
                if (next.HasValue)
                {
                    var delay = next.Value - DateTimeOffset.Now;
                    if (delay.TotalMilliseconds <= 0)   // prevent non-positive values from being passed into Timer
                    {
                        await ScheduleJob(cancellationToken);
                    }
                    _timer = new System.Timers.Timer(delay.TotalMilliseconds);
                    _timer.Elapsed += async (sender, args) =>
                    {
                        _timer.Dispose();  // reset and dispose timer
                        _timer = null;

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await DoWork(cancellationToken);
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await ScheduleJob(cancellationToken);    // reschedule next
                        }
                    };
                    _timer.Start();
                }

            }
            catch (Exception ex)
            {               
                await Task.CompletedTask;
            }
        }
        public virtual async Task DoWork(CancellationToken cancellationToken)
        {
            await Task.Delay(5000, cancellationToken);  // do the work
        }
        public virtual async Task SendNotification(CancellationToken cancellationToken, int notificationId, int countryId)
        {
            await Task.Delay(5000, cancellationToken);  // do the work
        }
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Stop();
            await Task.CompletedTask;
        }
        public virtual void Dispose()
        {
            _timer?.Dispose();
        }
        public virtual async Task NotificationSchedulePreviousJobs(CancellationToken cancellationToken)
        {
            _jobs.Clear();
            int notificationId = 1;
            var SendAt = DateTime.UtcNow;
            CancelScheduledJob(notificationId);
            try
            {
                var scheduledTime = SendAt;
                using (var scope = _scopeFactory.CreateScope())
                {
                    MyContext _db = scope.ServiceProvider.GetRequiredService<MyContext>();
                    DateTimeOffset scheduledDateTime = new DateTimeOffset(SendAt.Year, SendAt.Month, SendAt.Day, SendAt.TimeOfDay.Hours, SendAt.TimeOfDay.Minutes, 0, TimeSpan.Zero);
                    if (scheduledDateTime > DateTimeOffset.Now)
                    {
                        var delay = scheduledDateTime - DateTimeOffset.Now;
                        if (delay.TotalMilliseconds <= 0)   // prevent non-positive values from being passed into Timer
                        {
                            await NotificationSchedulePreviousJobs(cancellationToken);
                        }
                        var notificationSchedule = new NotoficationSchedule
                        {
                            NotificationId = notificationId,
                            _timerNotification = new System.Timers.Timer(delay.TotalMilliseconds)
                        };
                        notificationSchedule._timerNotification.Elapsed += async (sender, args) =>
                        {
                            notificationSchedule._timerNotification.Dispose();  // reset and dispose timer
                            notificationSchedule._timerNotification = null;

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await SendNotification(cancellationToken, notificationId, 1);
                            }
                        };
                        notificationSchedule._timerNotification.Start();
                        _jobs.Add(notificationSchedule);
                    }
                }
                await Task.CompletedTask;

            }
            catch (Exception ex)
            { }
        }
      
        public static void CancelScheduledJob(int notificationId)
        {
            var jobToCancel = _jobs.FirstOrDefault(x => x.NotificationId == notificationId);

            if (!ReferenceEquals(jobToCancel, null))
            {
                if (!ReferenceEquals(jobToCancel._timerNotification, null))
                {
                    jobToCancel._timerNotification.Stop();
                    jobToCancel._timerNotification.Dispose();
                    jobToCancel._timerNotification = null;
                }
                _jobs.Remove(jobToCancel);
            }
        }
    }
    
}
