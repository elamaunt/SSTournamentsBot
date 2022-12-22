using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class LeaveSlashCommand : SlashCommandBase
    {
        readonly IDataService _dataService;
        readonly TournamentApi _api;

        public LeaveSlashCommand(IDataService dataService, TournamentApi api)
        {
            _dataService = dataService;
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

            if (_api.TryLeaveUser(userData))
                await arg.RespondAsync($"Вы успешно покинули турнир.");
            else
                await arg.RespondAsync($"Вы не были зарегистрированы в следующем турнире.");
        }
    }
}
