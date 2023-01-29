using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class ViewSlashCommand : SlashCommandBase
    {
        public override string Name => "view";

        public override string DescriptionKey=> nameof(S.Commands_View);

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            if (!context.TournamentApi.IsTournamentStarted)
            {
                await arg.RespondAsync("Нет активного турнира.");
                return;
            }

            await arg.RespondWithFileAsync(new MemoryStream(await context.TournamentApi.RenderTournamentImage()), "tournament.png");
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
