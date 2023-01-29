using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class RegisterUserSlashCommand : SlashCommandBase
    {
        public override string Name => "register-user";
        public override string DescriptionKey=> nameof(S.Commands_RegisterUser);

        readonly IDataService _dataService;
        public RegisterUserSlashCommand(IDataService dataService)
        {
            _dataService = dataService;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var userOption = arg.Data.Options.First(x => x.Name == "user");
            var user = (IUser)userOption.Value;
            var userData = _dataService.FindUserByDiscordId(user.Id);

            if (userData != null)
            {
                await arg.RespondAsync($"Пользователь уже зарегистрирован в системе. SteamId = {userData.SteamId}");
                return;
            }

            var steamIdOption = arg.Data.Options.First(x => x.Name == "steam-id");
            var steamId = (ulong)(long)steamIdOption.Value;

            if (_dataService.StoreUsersSteamId(user.Id, steamId))
                await arg.RespondAsync($"Данные обновлены.");
            else
                await arg.RespondAsync($"Не удалось зарегистрировать пользователя.");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder
                .WithDefaultPermission(true)
                 .AddOption(new SlashCommandOptionBuilder()
                    .WithName("user")
                    .WithDescription("Пользователь")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.User))
                 .AddOption(new SlashCommandOptionBuilder()
                    .WithName("steam-id")
                    .WithDescription("Steam id")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer))
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
