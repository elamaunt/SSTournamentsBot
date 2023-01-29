using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class CheckInSlashCommand : SlashCommandBase
    {
        public override string Name => "checkin";
        public override string DescriptionKey=> nameof(S.Commands_Checkin);
        
        readonly IDataService _dataService;
        readonly IEventsTimeline _timeLine;
        readonly TournamentApi _api;

        public CheckInSlashCommand(IDataService dataService, IEventsTimeline timeLine, TournamentApi api)
        {
            _dataService = dataService;
            _timeLine = timeLine;
            _api = api;
        }
        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var userData =  _dataService.FindUserByDiscordId(arg.User.Id);

            if (userData == null)
            {
                await arg.RespondAsync(OfKey(nameof(S.CheckIn_YouAreNotRegisteredInTheSystem)).Build(culture));
                return;
            }

            var result = await _api.TryCheckInUser(userData.SteamId);

            if (result.IsNotRegisteredIn)
            {
                await arg.RespondAsync(OfKey(nameof(S.CheckIn_YouAreNotRegisteredInTournament)).Build(culture));
                return;
            }

            if (result.IsNotCheckInStageNow || result.IsNoTournament)
            {
                await arg.RespondAsync(OfKey(nameof(S.CheckIn_NotActiveNow)).Build(culture));
                return;
            }

            if (result.IsAlreadyCheckIned)
            {
                await arg.RespondAsync(OfKey(nameof(S.CheckIn_YouAreAlreadyCheckined)).Build(culture));
                return;
            }

            if (result.IsDone)
            {
                await arg.RespondAsync(OfKey(nameof(S.CheckIn_CheckInedSuccessfully)).Build(culture));
                return;
            }

            await arg.RespondAsync(OfKey(nameof(S.CheckIn_Error)).Build(culture));
        }
    }
}
