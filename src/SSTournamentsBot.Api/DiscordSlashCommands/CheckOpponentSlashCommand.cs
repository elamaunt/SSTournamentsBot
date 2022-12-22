using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class CheckOpponentSlashCommand : SlashCommandBase
    {
        public override string Name => "check-opponent";

        public override string Description => "Провести повторный чекин со своим оппонентом в контексте матча";

        public override Task Handle(SocketSlashCommand arg)
        {
            throw new NotImplementedException();
        }
    }
}
