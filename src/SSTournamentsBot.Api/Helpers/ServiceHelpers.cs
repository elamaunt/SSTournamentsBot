using SSTournamentsBot.Api.Services;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Helpers
{
    public static class ServiceHelpers
    {

        public static void ScheduleEveryDayTournament(this IEventsTimeline timeline)
        {
            var now = GetMoscowTime();

            var today = now
                .AddHours(-now.Hour)
                .AddMinutes(-now.Minute)
                .AddSeconds(-now.Second);

            var tournamentStartTime = today.AddHours(18);
            var checkinStartTime = tournamentStartTime.AddMinutes(-15);
            var preCheckinVoteStartTime = checkinStartTime.AddMinutes(-15);

            if (preCheckinVoteStartTime <= now)
            {
                today = today.AddDays(1);

                tournamentStartTime = today.AddHours(18);
                checkinStartTime = tournamentStartTime.AddMinutes(-30);
                preCheckinVoteStartTime = checkinStartTime.AddMinutes(-10);
            }

            timeline.RemoveAllEventsWithType(Event.StartPreCheckingTimeVote);
            timeline.AddPeriodicalEventOnSingleDayTime(Event.StartPreCheckingTimeVote, preCheckinVoteStartTime);
        }
    }
}
