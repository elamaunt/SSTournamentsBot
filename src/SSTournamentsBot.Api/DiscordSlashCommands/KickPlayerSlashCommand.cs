using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class KickPlayerSlashCommand : SlashCommandBase
    {
        public override string Name => "kick-player";
        public override string Description => "Исключить игрока из турнира";

        readonly IDataService _dataService;
        readonly TournamentApi _tournamentApi;
        public KickPlayerSlashCommand(IDataService dataService, TournamentApi tournamentApi)
        {
            _dataService = dataService;
            _tournamentApi = tournamentApi;
        }

        public override async Task Handle(SocketSlashCommand arg)
        {
            var userOption = arg.Data.Options.First(x => x.Name == "player");
            var user = (IUser)userOption.Value;
            var userData = _dataService.FindUserByDiscordId(user.Id);

            if (userData == null)
            {
                await arg.RespondAsync("Пользователь не зарегистрирован в системе.");
                return;
            }

            if (await _tournamentApi.TryLeaveUser(userData.DiscordId, userData.SteamId))
                await arg.RespondAsync($"Игрок {user.Username} покинул турнир.");
            else
                await arg.RespondAsync("Не удалось исключить игрока из турнира.");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("player")
                    .WithDescription("Игрок для исключения")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.User))
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
