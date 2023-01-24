using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
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

        public override async Task Handle(Context context, SocketSlashCommand arg)
        {
            var userData = _dataService.FindUserByDiscordId(arg.User.Id);

            if (userData == null)
            {
                await arg.RespondAsync(OfKey(nameof(S.Bot_YouAreNotRegistered)));
                return;
            }

            var result = await _api.TryLeaveUser(userData.DiscordId, userData.SteamId, TechnicalWinReason.OpponentsLeft);

            if (result.IsDone)
            {
                await arg.RespondAsync(OfKey(nameof(S.Leave_Successfull)));

                if (_api.IsTournamentStarted && _api.ActiveMatches.All(x => !x.Result.IsNotCompleted))
                    await _eventsHandler.DoCompleteStage(context.Name);
                return;
            }

            if (result.IsNotRegistered)
            {
                await arg.RespondAsync(OfKey(nameof(S.Bot_AreNotregisteredInEvent)));
                return;
            }

            if (result.IsNoTournament)
            {
                await arg.RespondAsync(OfKey(nameof(S.Bot_NoActiveTournament)));
                return;
            }

            if (result.IsAlreadyLeftBy)
            {
                var reason = ((LeaveUserResult.AlreadyLeftBy)result).Item;

                if (reason.IsVoting)
                {
                    await arg.RespondAsync(OfKey(nameof(S.Leave_YouAreAlreadyKickedByVoting)));
                    return;
                }

                if (reason.IsOpponentsLeft)
                {
                    await arg.RespondAsync(OfKey(nameof(S.Bot_AreAlreadyLeftTheEvent)));
                    return;
                }

                if (reason.IsOpponentsBan)
                {
                    await arg.RespondAsync(OfKey(nameof(S.Leave_ImposibleToLeaveCauseBanned)));
                    return;
                }

                if (reason.IsOpponentsKicked)
                {
                    await arg.RespondAsync(OfKey(nameof(S.Leave_ImposibleToLeaveCauseKickedByAdmin)));
                    return;
                }
            }
        }
    }
}
