using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class KickPlayerSlashCommand : SlashCommandBase
    {
        public override string Name => "kick-player";
        public override string Description => "Исключить игрока из турнира";

        readonly IDataService _dataService;
        readonly ITournamentEventsHandler _eventsHandler;
        readonly TournamentApi _tournamentApi;
        public KickPlayerSlashCommand(IDataService dataService, ITournamentEventsHandler tournamentEventsHandler, TournamentApi tournamentApi)
        {
            _dataService = dataService;
            _eventsHandler = tournamentEventsHandler;
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

            if (await _tournamentApi.TryLeaveUser(userData.DiscordId, userData.SteamId, TechnicalWinReason.OpponentsKicked))
            {
                await arg.RespondAsync($"Игрок **{user.Username}** покинул турнир.");

                if (_tournamentApi.IsTounamentStarted)
                {
                    if (_tournamentApi.ActiveMatches.All(x => !x.Result.IsNotCompleted))
                        _eventsHandler.DoCompleteStage();
                }
                else
                {
                    if (_tournamentApi.IsCheckinStage && _tournamentApi.IsAllPlayersCheckIned())
                        _eventsHandler.DoStartCurrentTournament();
                }
            }
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
