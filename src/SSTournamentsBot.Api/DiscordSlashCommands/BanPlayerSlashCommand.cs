using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class BanPlayerSlashCommand : SlashCommandBase
    {
        public override string Name => "ban-player";
        public override string Description => "Исключить игрока из турнира";

        readonly IDataService _dataService;
        readonly IBotApi _botApi;
        readonly TournamentApi _tournamentApi;
        public BanPlayerSlashCommand(IDataService dataService, IBotApi botApi, TournamentApi tournamentApi)
        {
            _dataService = dataService;
            _botApi = botApi;
            _tournamentApi = tournamentApi;
        }

        public override async Task Handle(SocketSlashCommand arg)
        {
            var userOption = arg.Data.Options.FirstOrDefault(x => x.Name == "player");

            var user = (IUser)userOption.Value;
            var userData = _dataService.FindUserByDiscordId(user.Id);

            if (userData == null)
            {
                await arg.RespondAsync("Пользователь не зарегистрирован в системе.");
                return;
            }

            if (await _tournamentApi.TryLeaveUser(userData.DiscordId, userData.SteamId, TechnicalWinReason.OpponentsBan))
            {
                var mention = await _botApi.GetMention(userData.DiscordId);
                await _botApi.SendMessage($"{mention} исключен из турнира.", GuildThread.EventsTape | GuildThread.TournamentChat);
            }

            userData.Banned = true;
            _dataService.UpdateUser(userData);
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("player")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.User))
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
