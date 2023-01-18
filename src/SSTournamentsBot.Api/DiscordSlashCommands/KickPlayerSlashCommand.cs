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

            var result = await _tournamentApi.TryLeaveUser(userData.DiscordId, userData.SteamId, TechnicalWinReason.OpponentsKicked);

            if (result.IsDone)
            {
                await arg.RespondAsync($"Игрок **{user.Username}** покинул турнир.");

                if (_tournamentApi.IsTournamentStarted)
                {
                    if (_tournamentApi.ActiveMatches.All(x => !x.Result.IsNotCompleted))
                        await _eventsHandler.DoCompleteStage();
                }
                else
                {
                    if (_tournamentApi.IsCheckinStage && _tournamentApi.IsAllPlayersCheckIned)
                        await _eventsHandler.DoStartCurrentTournament();
                }
                return;
            }

            if (result.IsNoTournament)
            {
                await arg.RespondAsync("Нет активного турнира");
                return;
            }

            if (result.IsNotRegistered)
            {
                await arg.RespondAsync("Нельзя исключить игрока из турнира, в котором он не зарегистрирован.");
                return;
            }

            if (result.IsAlreadyLeftBy)
            {
                var reason = ((LeaveUserResult.AlreadyLeftBy)result).Item;

                if (reason.IsVoting)
                {
                    await arg.RespondAsync("Вы не можете покинуть турнир, так как вы уже покинули его путем голосования.");
                    return;
                }

                if (reason.IsOpponentsLeft)
                {
                    await arg.RespondAsync("Вы уже покинули этот турнир.");
                    return;
                }

                if (reason.IsOpponentsBan)
                {
                    await arg.RespondAsync("Вы не можете покинуть турнир, так как вы были забанены.");
                    return;
                }

                if (reason.IsOpponentsKicked)
                {
                    await arg.RespondAsync("Вы не можете покинуть турнир, так как вы были исключены из него администрацией.");
                    return;
                }
            }

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
