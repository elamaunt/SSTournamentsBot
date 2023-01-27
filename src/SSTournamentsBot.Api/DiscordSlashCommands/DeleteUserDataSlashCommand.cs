using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class DeleteUserDataSlashCommand : SlashCommandBase
    {
        readonly IDataService _dataService;

        public DeleteUserDataSlashCommand(IDataService dataService)
        {
            _dataService = dataService;
        }

        public override string Name => "delete-user-data";

        public override string Description => "Удаляет профиль пользователя (для админов)";

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var userOption = arg.Data.Options.First(x => x.Name == "player");
            var user = (IUser)userOption.Value;

            if (_dataService.DeleteUser(user.Id))
                await arg.RespondAsync($"Пользователь **{user.Username}** успешно удален из базы.");
            else
                await arg.RespondAsync("Пользователь не зарегистрирован в системе.");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("player")
                    .WithDescription("Игрок")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.User))
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
