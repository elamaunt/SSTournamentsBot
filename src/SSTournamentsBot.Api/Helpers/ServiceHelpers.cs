using SSTournamentsBot.Api.DataDomain;
using SSTournamentsBot.Api.Services;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            return ev.ToString();
        }

        /*public static void ScheduleEveryDayTournament(this IEventsTimeline timeline, TournamentEventsOptions options)
        {
            var now = GetMoscowTime();

            var today = now
                .AddHours(-now.Hour)
                .AddMinutes(-now.Minute)
                .AddSeconds(-now.Second);

            var tournamentStartTime = today.AddHours(options.TournamentStartHour);
            var checkinStartTime = tournamentStartTime.AddMinutes(-options.CheckInTimeoutMinutes);
            var preCheckinVoteStartTime = checkinStartTime.AddMinutes(-options.PreCheckinTimeVotingOffsetMinutes);

            if (preCheckinVoteStartTime <= now)
            {
                today = today.AddDays(1);

                tournamentStartTime = today.AddHours(options.TournamentStartHour);
                checkinStartTime = tournamentStartTime.AddMinutes(-options.CheckInTimeoutMinutes);
                preCheckinVoteStartTime = checkinStartTime.AddMinutes(-options.PreCheckinTimeVotingOffsetMinutes);
            }

            timeline.RemoveAllEventsWithType(Event.StartPreCheckingTimeVote);
            timeline.AddPeriodicalEventOnSingleDayTime(Event.StartPreCheckingTimeVote, preCheckinVoteStartTime);
        }*/

        public static async Task RefreshLeaders(Context context, IBotApi botApi, IDataService dataService, bool notify = true)
        {
            await PrintAndUpdateLeaders(context, botApi, notify, dataService.LoadLeaders());
        }
        public static async Task RefreshLeadersV2(Context context, IBotApi botApi, IDataService dataService, bool notify = true)
        {
            var leaders = dataService.EnumerateAllUsers()
                .Where(x => x.Score != 0)
                .OrderByDescending(x => x.Score)
                .ToArray();

            await PrintAndUpdateLeaders(context, botApi, notify, leaders);
        }

        private static async Task PrintAndUpdateLeaders(Context context, IBotApi botApi, bool notify, UserData[] leaders)
        {
            var builder = new StringBuilder();
            builder.AppendLine("--- __**Таблица лидеров**__ ---");
            builder.AppendLine();

            for (int i = 0; i < leaders.Length; i++)
            {
                var user = leaders[i];

                builder.AppendLine($"{i + 1}. {user.Score}   {await botApi.GetUserName(context, user.DiscordId)}");
            }

            builder.AppendLine();

            await botApi.ModifyLastMessage(context, builder.ToString(), GuildThread.Leaderboard);

            builder.Clear();

            if (notify)
                await botApi.SendMessage(context, "Таблица лидеров была обновлена.", GuildThread.EventsTape | GuildThread.TournamentChat);
        }
    }
}
