using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class MyIdSlashCommand : SlashCommandBase
    {
        public override string Name => "my-id";
        public override string DescriptionKey=> nameof(S.Commands_MyId);

        public override Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            return arg.RespondAsync($"Ваш id = {arg.User.Id}");
        }
    }
}
