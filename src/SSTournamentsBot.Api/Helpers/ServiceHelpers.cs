﻿using SSTournamentsBot.Api.DataDomain;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Resources;
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

        public static string PrettyPrint(this Event ev, bool isRussian)
        {
            if (isRussian)
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

            if (ev.IsCompleteStage)
                return "Stage completion";
            if (ev.IsCompleteVoting)
                return "Voting copmletion";
            if (ev.IsStartCheckIn)
                return "Checkin opening";
            if (ev.IsStartNextStage)
                return "Next stage opening";
            if (ev.IsStartCurrentTournament)
                return "Event start";
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

        public static async Task RefreshLeadersVanilla(Context context, IDataService dataService, bool notify = true)
        {
            await PrintAndUpdateLeadersVanilla(context, notify, dataService.LoadLeadersVanilla());
        }

        public static async Task RefreshLeadersVanillaV2(Context context, IDataService dataService, bool notify = true)
        {
            var leaders = dataService.EnumerateAllUsers()
                .Where(x => x.Score != 0)
                .OrderByDescending(x => x.Score)
                .ToArray();

            await PrintAndUpdateLeadersVanilla(context, notify, leaders);
        }

        private static async Task PrintAndUpdateLeadersVanilla(Context context, bool notify, UserData[] leaders)
        {
            var text = new CompoundText();
            text.AppendLine(Text.OfKey(nameof(S.Events_LeaderBoardHeader)));
            text.AppendLine(Text.OfValue(""));

            for (int i = 0; i < leaders.Length; i++)
            {
                var user = leaders[i];

                text.AppendLine(Text.OfValue($"{i + 1}. {user.Score}   {await context.BotApi.GetUserName(context, user.DiscordId)}"));
            }

            text.AppendLine(Text.OfValue(""));

            await context.BotApi.ModifyLastMessage(context, text, GuildThread.Leaderboard);

            if (notify)
                await context.BotApi.SendMessage(context, Text.OfKey(nameof(S.Events_LeaderboardHasBeenUpdated)), GuildThread.EventsTape | GuildThread.TournamentChat);
        }

        public static async Task RefreshLeadersOtherMods(Context context, IDataService dataService, bool notify = true)
        {
            await PrintAndUpdateLeadersOtherMods(context, notify, dataService.LoadLeaders(context.Name));
        }


        private static async Task PrintAndUpdateLeadersOtherMods(Context context, bool notify, UserInActivityModel[] leaders)
        {
            var text = new CompoundText();
            text.AppendLine(Text.OfKey(nameof(S.Events_LeaderBoardHeader)));
            text.AppendLine(Text.OfValue(""));

            for (int i = 0; i < leaders.Length; i++)
            {
                var user = leaders[i];

                text.AppendLine(Text.OfValue($"{i + 1}. {user.Score}   {await context.BotApi.GetUserName(context, user.DiscordId)}"));
            }

            text.AppendLine(Text.OfValue(""));

            await context.BotApi.ModifyLastMessage(context, text, GuildThread.Leaderboard);

            if (notify)
                await context.BotApi.SendMessage(context, Text.OfKey(nameof(S.Events_LeaderboardHasBeenUpdated)), GuildThread.EventsTape | GuildThread.TournamentChat);
        }
    }
}
