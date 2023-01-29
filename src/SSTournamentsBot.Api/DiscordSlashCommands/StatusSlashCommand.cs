using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class StatusSlashCommand : SlashCommandBase
    {
        public override string Name => "status";

        public override string DescriptionKey=> nameof(S.Commands_Status);

        public override Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
