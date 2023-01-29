using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
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
        readonly TournamentApi _tournamentApi;
        readonly IEventsTimeline _timeline;
        readonly IBotApi _botApi;
        ulong[] Mentions => _tournamentApi.RegisteredPlayers.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray();

        public StartSlashCommand(IEventsTimeline timeline, IBotApi botApi, TournamentApi tournamentApi, IOptions<TournamentEventsOptions> options)
        {
            _timeline = timeline;
            _botApi = botApi;
            _tournamentApi = tournamentApi;
            _options = options.Value;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var result = await _tournamentApi.TryStartTheCheckIn();

            if (result.IsDone)
            {
                _timeline.RemoveAllEventsWithType(Event.NewStartCheckIn(context.Name));
                _timeline.RemoveAllEventsWithType(Event.NewStartCurrentTournament(context.Name));

               // await _botApi.SendMessage(context, $"Внимание! Началась стадия чекина на турнир.\nВсем участникам нужно выполнить команду __**/checkin**__ на турнирном канале для подтверждения своего участия.\nДлительность чек-ина {_options.CheckInTimeoutMinutes} минут.\nРегистрация новых участников позволяется и не требует чекина.", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);

                _timeline.AddOneTimeEventAfterTime(Event.NewStartCurrentTournament(context.Name), TimeSpan.FromMinutes(15));

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
