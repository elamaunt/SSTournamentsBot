using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class PlayersShashCommand : SlashCommandBase
    {
        public override string Name => "players";
        public override string Description => "Вывести список зарегистрированных игроков";

        protected readonly TournamentApi _tournamentApi;
        public PlayersShashCommand(TournamentApi tournamentApi)
        {
            _tournamentApi = tournamentApi;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var builder = new StringBuilder();

            var players = _tournamentApi.RegisteredPlayers;

            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                builder.AppendLine($"{i + 1}. **{player.Name}** ({player.Race})");
            }

            if (builder.Length > 0)
                await arg.RespondAsync(builder.ToString());
            else
                await arg.RespondAsync(OfKey(nameof(S.Players_NobodyRegistered)).Build(culture));
        }

    }
}
