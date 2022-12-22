using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class KickBotsSlashCommand : SlashCommandBase
    {
        public override string Name => "kick-bots";
        public override string Description => "Исключить всех ботов из турнира (для тестов)";

        public override Task Handle(SocketSlashCommand arg)
        {
            throw new NotImplementedException();
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
