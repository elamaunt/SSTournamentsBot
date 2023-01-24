using Discord.WebSocket;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class InfoSlashCommand : SlashCommandBase
    {
        readonly TournamentApi _api;

        public InfoSlashCommand(TournamentApi api)
        {
            _api = api;
        }

        public override string Name => "info";

        public override string Description => "Получить текущую информацию о следующем турнире";

        public override async Task Handle(Context context, SocketSlashCommand arg)
        {
            var infoBuilder = new CompoundText();

            if (_api.IsTournamentStarted)
            {
                infoBuilder.AppendLine(OfKey(nameof(S.Bot_TournamentInProgress)));
            }
            else
            {
                infoBuilder.AppendLine(OfKey(nameof(S.Bot_TournamentNotStartedYet)));
            }

            infoBuilder.AppendLine(OfKey(nameof(S.Info_TournamentType)).Format(_api.TournamentType?.ToString() ?? "Неопределен"));

            var players = _api.RegisteredPlayers;

            if (players.Length == 0)
                infoBuilder.AppendLine(OfKey(nameof(S.Info_NobodyRegisteredMessage)));
            else
                infoBuilder.AppendLine(OfKey(nameof(S.Info_RegisteredPlayers)).Format(players.Length));

            await arg.RespondAsync(infoBuilder.ToString());
        }
    }
}
