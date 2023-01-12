using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class SetWaitingRoleEnabledSlashCommand : SlashCommandBase
    {
        public override string Name => "set-waiting-role-enabled";
        public override string Description => "Получить/Убрать у себя роль 'Жду турниров'";

        readonly IBotApi _botApi;
        public SetWaitingRoleEnabledSlashCommand(IBotApi botApi)
        {
            _botApi = botApi;
        }

        public override async Task Handle(SocketSlashCommand arg)
        {
            var valueOption = arg.Data.Options.First(x => x.Name == "value");

            await arg.DeferAsync();

            bool enabled;

            if (valueOption != null)
                enabled = await _botApi.ToggleWaitingRole((bool)valueOption.Value);
            else
                enabled = await _botApi.ToggleWaitingRole(null);

            if (enabled)
                await arg.ModifyOriginalResponseAsync(x => x.Content = $"Вы будете получать уведомления о начинающихся турнирах");
            else
                await arg.ModifyOriginalResponseAsync(x => x.Content = $"Турниры вас не побеспокоят");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder
                 .AddOption(new SlashCommandOptionBuilder()
                    .WithName("value")
                    .WithDescription("Активно")
                    .WithRequired(false)
                    .WithType(ApplicationCommandOptionType.Boolean))
                .WithDMPermission(true);
        }
    }
}
