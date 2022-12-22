
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class CheckInSlashCommand : SlashCommandBase
    {
        public override string Name => "checkin";
        public override string Description => "Подтвердить свое участие";
        
        readonly IDataService _dataService;
        readonly TournamentApi _api;

        public CheckInSlashCommand(IDataService dataService, TournamentApi api)
        {
            _dataService = dataService;
            _api = api;
        }
        public override async Task Handle(SocketSlashCommand arg)
        {
            var userData = _dataService.FindUserByDiscordId(arg.User.Id);

            if (userData == null)
            {
                await arg.RespondAsync("Вы не зарегистрированы в системе.");
                return;
            }

            var result = _api.ChechInUser(userData);

            if (result.IsNotCheckInStageNow)
            {
                await arg.RespondAsync($"В данный момент чекин не проводится");
                return;
            }

            if (result.IsAlreadyCheckIned)
            {
                await arg.RespondAsync($"Вы уже подтведили свое участие");
                return;
            }

            if (result.IsDone)
            {
                await arg.RespondAsync($"Вы успешно подтведили свое участие! ");
                return;
            }
        }
    }
}
