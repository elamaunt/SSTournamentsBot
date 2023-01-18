
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class CheckInSlashCommand : SlashCommandBase
    {
        public override string Name => "checkin";
        public override string Description => "Подтвердить свое участие";
        
        readonly IDataService _dataService;
        readonly IEventsTimeline _timeLine;
        readonly TournamentApi _api;

        public CheckInSlashCommand(IDataService dataService, IEventsTimeline timeLine, TournamentApi api)
        {
            _dataService = dataService;
            _timeLine = timeLine;
            _api = api;
        }
        public override async Task Handle(SocketSlashCommand arg)
        {
            var userData =  _dataService.FindUserByDiscordId(arg.User.Id);

            if (userData == null)
            {
                await arg.RespondAsync("> Вы не зарегистрированы в системе. Используйте команду */play*.");
                return;
            }

            var result = await _api.TryCheckInUser(userData.SteamId);

            if (result.IsNotRegisteredIn)
            {
                await arg.RespondAsync($"> Вы не регистрировались на текущий турнир.");
                return;
            }

            if (result.IsNotCheckInStageNow || result.IsNoTournament)
            {
                await arg.RespondAsync($"> В данный момент чекин не проводится.");
                return;
            }

            if (result.IsAlreadyCheckIned)
            {
                await arg.RespondAsync($"> Вы уже подтведили свое участие.");
                return;
            }

            if (result.IsDone)
            {
                await arg.RespondAsync($"> Вы успешно подтведили свое участие!");
                return;
            }

            await arg.RespondAsync("> Произошла ошибка");
        }
    }
}
