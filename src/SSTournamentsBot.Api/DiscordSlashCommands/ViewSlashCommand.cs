using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.IO;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class ViewSlashCommand : SlashCommandBase
    {
        private TournamentApi _api;

        public ViewSlashCommand(TournamentApi api)
        {
            _api = api;
        }

        public override string Name => "view";

        public override string Description => "Выводит текущую сетку турнира";

        public override Task Handle(SocketSlashCommand arg)
        {
            return arg.RespondWithFileAsync(new MemoryStream(_api.RenderTournamentImage()), "tournament.png");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder
                .WithDefaultPermission(true)
                .WithDefaultMemberPermissions(GuildPermission.Administrator | GuildPermission.ModerateMembers)
                .WithDMPermission(true);
        }
    }
}
