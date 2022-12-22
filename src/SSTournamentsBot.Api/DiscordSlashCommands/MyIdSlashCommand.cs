using Discord.WebSocket;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class MyIdSlashCommand : SlashCommandBase
    {
        public override string Name => "my-id";
        public override string Description => "Выводит Ваш discord user id";

        public override Task Handle(SocketSlashCommand arg)
        {
            return arg.RespondAsync($"Ваш id = {arg.User.Id}");
        }
    }
}
