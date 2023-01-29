using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class DropTournamentSlashCommand : SlashCommandBase
    {
        public override string Name => "drop-tournament";
        public override string DescriptionKey=> nameof(S.Commands_Drop);

        readonly IEventsTimeline _timeline;

        public DropTournamentSlashCommand(IEventsTimeline timeline)
        {
            _timeline = timeline;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            _timeline.RemoveAllEvents(context.Name);
            await context.TournamentApi.DropTournament();
            await context.EventsHandler.DoCompleteVoting(context.Name);
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
