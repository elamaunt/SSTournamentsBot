using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class SetUsersScoreSlashCommand : SlashCommandBase
    {
        public override string Name => "set-users-score";
        public override string DescriptionKey=> nameof(S.Commands_SetUsersScore);

        readonly IDataService _dataService;

        public SetUsersScoreSlashCommand(IDataService dataService)
        {
            _dataService = dataService;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var userOption = arg.Data.Options.First(x => x.Name == "user");
            var user = (IUser)userOption.Value;
            var userData = _dataService.FindUserByDiscordId(user.Id);

            if (userData == null)
            {
                await arg.RespondAsync("Пользователь не зарегистрирован в системе.");
                return;
            }

            var scoreOption = arg.Data.Options.First(x => x.Name == "score");

            userData.Score = (int)(long)scoreOption.Value;
            if (_dataService.UpdateUser(userData))
                await arg.RespondAsync($"Данные обновлены. Текущий рейтинг: {userData.Score}");
            else
                await arg.RespondAsync($"Не удалось обновить рейтинг пользователя.");
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
                    .WithName("score")
                    .WithDescription("Рейтинг")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer))
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
