using SSTournamentsBot.Api.Services;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Helpers
{
    public static class ServiceHelpers
    {
        public static string BuildStatsUrl(this ulong steamId)
        {
            return $"https://dowstats.ru/player.php?sid={steamId}&server=steam#tab0";
        }

        public static string PrettyPrint(this Event ev)
        {
            if (ev.IsCompleteStage)
                return "Завершение стадии";
            if (ev.IsCompleteVoting)
                return "Завершение голосования";
            if (ev.IsStartCheckIn)
                return "Открытие чекина";
            if (ev.IsStartNextStage)
                return "Начало следующей стадии";
            if (ev.IsStartCurrentTournament)
                return "Начало турнира";
            if (ev.IsStartPreCheckingTimeVote)
                return "Предтурнирное голосование";
            return ev.ToString();
        }

        public static void ScheduleEveryDayTournament(this IEventsTimeline timeline, TournamentEventsOptions options)
        {
            var now = GetMoscowTime();

            var today = now
                .AddHours(-now.Hour)
                .AddMinutes(-now.Minute)
                .AddSeconds(-now.Second);

            var tournamentStartTime = today.AddHours(18);
            var checkinStartTime = tournamentStartTime.AddMinutes(-options.CheckInTimeoutMinutes);
            var preCheckinVoteStartTime = checkinStartTime.AddMinutes(-options.PreCheckinTimeVotingOffsetMinutes);

            if (preCheckinVoteStartTime <= now)
            {
                today = today.AddDays(1);

                tournamentStartTime = today.AddHours(18);
                checkinStartTime = tournamentStartTime.AddMinutes(-options.CheckInTimeoutMinutes);
                preCheckinVoteStartTime = checkinStartTime.AddMinutes(-options.PreCheckinTimeVotingOffsetMinutes);
            }

            timeline.RemoveAllEventsWithType(Event.StartPreCheckingTimeVote);
            timeline.AddPeriodicalEventOnSingleDayTime(Event.StartPreCheckingTimeVote, preCheckinVoteStartTime);
        }
    }
}
