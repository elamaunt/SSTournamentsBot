using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class WaitSlashCommand : SlashCommandBase
    {
        public override string Name => "wait";
        public override string Description => "Получить/Убрать у себя роль 'Жду турниров'";

        readonly IBotApi _botApi;
        public WaitSlashCommand(IBotApi botApi)
        {
            _botApi = botApi;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var valueOption = arg.Data.Options.FirstOrDefault(x => x.Name == "value");

            await arg.DeferAsync();

            bool enabled;

            if (valueOption != null)
                enabled = await _botApi.ToggleWaitingRole(context, arg.User.Id, (bool)valueOption.Value);
            else
                enabled = await _botApi.ToggleWaitingRole(context, arg.User.Id, null);

            if (enabled)
                await arg.ModifyOriginalResponseAsync(x => x.Content = OfKey(S.Wait_Enabled).Build(culture));
            else
                await arg.ModifyOriginalResponseAsync(x => x.Content = OfKey(S.Wait_Disabled).Build(culture));
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
