using Discord.WebSocket;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class StatusSlashCommand : SlashCommandBase
    {
        public override string Name => "status";

        public override string Description => "Узнать ваш статус в текущем турнире";

        public override Task Handle(SocketSlashCommand arg)
        {
            throw new System.NotImplementedException();
        }
    }
}
