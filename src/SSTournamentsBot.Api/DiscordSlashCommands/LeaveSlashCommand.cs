using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class LeaveSlashCommand : SlashCommandBase
    {
        readonly IDataService _dataService;
        readonly ITournamentEventsHandler _eventsHandler;
        readonly TournamentApi _api;

        public LeaveSlashCommand(IDataService dataService, ITournamentEventsHandler tournamentEventsHandler, TournamentApi api)
        {
            _dataService = dataService;
            _eventsHandler = tournamentEventsHandler;
            _api = api;
        }

        public override string Name => "leave";
        public override string Description => "Покинуть турнир или исключить себя из регистрации";

        public override async Task Handle(SocketSlashCommand arg)
        {
            var userData = _dataService.FindUserByDiscordId(arg.User.Id);

            if (userData == null)
            {
                await arg.RespondAsync("> Вы не зарегистрированы в системе.");
                return;
            }

            var result = await _api.TryLeaveUser(userData.DiscordId, userData.SteamId, TechnicalWinReason.OpponentsLeft);

            if (result.IsDone)
            {
                await arg.RespondAsync($"> Вы успешно покинули турнир.");

                if (_api.IsTounamentStarted && _api.ActiveMatches.All(x => !x.Result.IsNotCompleted))
                    await _eventsHandler.DoCompleteStage();
                return;
            }

            if (result.IsNotRegistered)
            {
                await arg.RespondAsync($"> Вы не были зарегистрированы в следующем турнире.");
                return;
            }

            if (result.IsNoTournament)
            {
                await arg.RespondAsync($"> В данный момент нет турнира.");
                return;
            }

            if (result.IsAlreadyLeftBy)
            {
                var reason = ((LeaveUserResult.AlreadyLeftBy)result).Item;

                if (reason.IsVoting)
                {
                    await arg.RespondAsync("> Вы не можете покинуть турнир, так как вы уже покинули его путем голосования.");
                    return;
                }

                if (reason.IsOpponentsLeft)
                {
                    await arg.RespondAsync("> Вы уже покинули этот турнир.");
                    return;
                }

                if (reason.IsOpponentsBan)
                {
                    await arg.RespondAsync("> Вы не можете покинуть турнир, так как вы были забанены.");
                    return;
                }

                if (reason.IsOpponentsKicked)
                {
                    await arg.RespondAsync("> Вы не можете покинуть турнир, так как вы были исключены из него администрацией.");
                    return;
                }
            }
        }
    }
}
