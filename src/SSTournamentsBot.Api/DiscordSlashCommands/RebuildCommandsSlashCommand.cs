using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class RebuildCommandsSlashCommand : SlashCommandBase
    {
        private DiscordCommandsHandler _handler;

        public RebuildCommandsSlashCommand(DiscordCommandsHandler handler)
        {
            _handler = handler;
        }

        public override string Name => "rebuild-commands";

        public override string Description => "Перестраивает все команды бота (для админов)";

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            await arg.DeferAsync();
            await _handler.RebuildCommands();
            await arg.ModifyOriginalResponseAsync(x => x.Content = "Команды перестроены");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder
                .WithDefaultPermission(true)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
