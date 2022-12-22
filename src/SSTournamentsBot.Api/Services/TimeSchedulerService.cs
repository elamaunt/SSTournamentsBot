using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class TimeSchedulerService : IHostedService, IDisposable
    {
        private readonly ILogger<TimeSchedulerService> _logger;
        private readonly IEventsHandler _handler;
        private readonly IEventsTimeline _eventsTimeLine;
        private Timer _timer = null;

        DateTime _checkPoint;
        public TimeSchedulerService(ILogger<TimeSchedulerService> logger, IEventsHandler handler, IEventsTimeline eventsTimeLine)
        {
            _logger = logger;
            _handler = handler;
            _eventsTimeLine = eventsTimeLine;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _checkPoint = GetMoscowTime();
            _logger.LogInformation("Time Scheduler Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            var before = _checkPoint;
            var now = GetMoscowTime();
            var period = now - before;

            var events = _eventsTimeLine.GetEventsRaisedInPeriod(before, period, true);

            for (int i = 0; i < events.Length; i++)
                SwitchEvent(events[i], _handler);

            _checkPoint = now;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Time Scheduler Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
