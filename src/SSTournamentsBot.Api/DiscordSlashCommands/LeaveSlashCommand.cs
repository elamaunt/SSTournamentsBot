using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class LeaveSlashCommand : SlashCommandBase
    {
        readonly IDataService _dataService;

        public LeaveSlashCommand(IDataService dataService)
        {
            _dataService = dataService;
        }

        public override string Name => "leave";
        public override string DescriptionKey => nameof(S.Commands_Leave);

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var userData = _dataService.FindUserByDiscordId(arg.User.Id);

            if (userData == null)
            {
                await arg.RespondAsync(OfKey(nameof(S.Bot_YouAreNotRegistered)).Build(culture));
                return;
            }

            var result = await context.TournamentApi.TryLeaveUser(userData.DiscordId, userData.SteamId, TechnicalWinReason.OpponentsLeft);

            if (result.IsDone)
            {
                await arg.RespondAsync(OfKey(nameof(S.Leave_Successfull)).Build(culture));

                if (context.TournamentApi.IsTournamentStarted && context.TournamentApi.ActiveMatches.All(x => !x.Result.IsNotCompleted))
                    await context.EventsHandler.DoCompleteStage(context.Name);
                return;
            }

            if (result.IsNotRegistered)
            {
                await arg.RespondAsync(OfKey(nameof(S.Bot_AreNotregisteredInEvent)).Build(culture));
                return;
            }

            if (result.IsNoTournament)
            {
                await arg.RespondAsync(OfKey(nameof(S.Bot_NoActiveTournament)).Build(culture));
                return;
            }

            if (result.IsAlreadyLeftBy)
            {
                var reason = ((LeaveUserResult.AlreadyLeftBy)result).Item;

                if (reason.IsVoting)
                {
                    await arg.RespondAsync(OfKey(nameof(S.Leave_YouAreAlreadyKickedByVoting)).Build(culture));
                    return;
                }

                if (reason.IsOpponentsLeft)
                {
                    await arg.RespondAsync(OfKey(nameof(S.Bot_AreAlreadyLeftTheEvent)).Build(culture));
                    return;
                }

                if (reason.IsOpponentsBan)
                {
                    await arg.RespondAsync(OfKey(nameof(S.Leave_ImposibleToLeaveCauseBanned)).Build(culture));
                    return;
                }

                if (reason.IsOpponentsKicked)
                {
                    await arg.RespondAsync(OfKey(nameof(S.Leave_ImposibleToLeaveCauseKickedByAdmin)).Build(culture));
                    return;
                }
            }
        }
    }
}
