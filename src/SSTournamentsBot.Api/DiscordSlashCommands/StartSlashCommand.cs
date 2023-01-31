using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class StartSlashCommand : SlashCommandBase
    {
        public override string Name => "start";
        public override string DescriptionKey=> nameof(S.Commands_Start);

        readonly TournamentEventsOptions _options;
        readonly IEventsTimeline _timeline;

        public StartSlashCommand(IEventsTimeline timeline, IOptions<TournamentEventsOptions> options)
        {
            _timeline = timeline;
            _options = options.Value;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            ulong[] AllPlayersMentions() => context.TournamentApi.RegisteredPlayers.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray();

            var result = await context.TournamentApi.TryStartTheCheckIn();

            if (result.IsDone)
            {
                _timeline.RemoveAllEventsWithType(context.Name, Event.NewStartCheckIn(context.Name));
                _timeline.RemoveAllEventsWithType(context.Name, Event.NewStartCurrentTournament(context.Name));

                await context.BotApi.MentionWaitingRole(context, GuildThread.EventsTape | GuildThread.TournamentChat);
                await context.BotApi.SendMessage(context, Text.OfKey(nameof(S.Events_ActivityCheckinStarted)).Format(context.TournamentApi.TournamentType, context.TournamentApi.Id, _options.CheckInTimeoutMinutes), GuildThread.EventsTape | GuildThread.TournamentChat, AllPlayersMentions());

                _timeline.AddOneTimeEventAfterTime(context.Name, Event.NewStartCurrentTournament(context.Name), TimeSpan.FromMinutes(_options.CheckInTimeoutMinutes));

                await arg.RespondAsync("Турнир успешно запущен.");
                return;
            }

            if (result.IsNotEnoughPlayers)
            {
                await arg.RespondAsync("Недостаточно участников для начала.");
                return;
            }

            if (result.IsAlreadyStarted)
            {
                await arg.RespondAsync("Турнир уже идет.");
                return;
            }

            if (result.IsNoTournament)
            {
                await arg.RespondAsync("Турнир не создан.");
                return;
            }

            await arg.RespondAsync("Ошибка.");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
