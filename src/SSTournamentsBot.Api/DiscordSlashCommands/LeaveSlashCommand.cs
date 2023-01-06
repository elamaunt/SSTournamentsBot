using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Threading.Tasks;
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
                await arg.RespondAsync("Вы не зарегистрированы в системе.");
                return;
            }

            if (await _api.TryLeaveUser(userData.DiscordId, userData.SteamId))
            {
                await arg.RespondAsync($"Вы успешно покинули турнир.");

                if (_api.ActiveMatches.All(x => !x.Result.IsNotCompleted))
                    _eventsHandler.DoCompleteStage();
            }
            else
                await arg.RespondAsync($"Вы не были зарегистрированы в следующем турнире.");
        }
    }
}
