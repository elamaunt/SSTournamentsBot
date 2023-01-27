using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class DropTournamentSlashCommand : SlashCommandBase
    {
        public override string Name => "drop-tournament";
        public override string Description => "Сбросить текущее состояние турнира (для админов)";

        readonly TournamentApi _tournamentApi;
        readonly IEventsTimeline _timeline;
        readonly ITournamentEventsHandler _handler;

        public DropTournamentSlashCommand(TournamentApi tournamentApi, IEventsTimeline timeline, ITournamentEventsHandler handler)
        {
            _tournamentApi = tournamentApi;
            _timeline = timeline;
            _handler = handler;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            _timeline.RemoveAllEvents();
            await _tournamentApi.DropTournament();
            await _handler.DoCompleteVoting(context.Name);
            await arg.RespondAsync("Сброс состояния выполнен.");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
