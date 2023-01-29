using Discord.WebSocket;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class InfoSlashCommand : SlashCommandBase
    {
        public override string Name => "info";

        public override string DescriptionKey=> nameof(S.Commands_Info);

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var infoBuilder = new CompoundText();

            if (context.TournamentApi.IsTournamentStarted)
            {
                infoBuilder.AppendLine(OfKey(nameof(S.Bot_TournamentInProgress)));
            }
            else
            {
                infoBuilder.AppendLine(OfKey(nameof(S.Bot_TournamentNotStartedYet)));
            }

            infoBuilder.AppendLine(OfKey(nameof(S.Info_TournamentType)).Format(context.TournamentApi.TournamentType?.ToString() ?? "Неопределен"));

            var players = context.TournamentApi.RegisteredPlayers;

            if (players.Length == 0)
                infoBuilder.AppendLine(OfKey(nameof(S.Info_NobodyRegisteredMessage)));
            else
                infoBuilder.AppendLine(OfKey(nameof(S.Info_RegisteredPlayers)).Format(players.Length));

            await arg.RespondAsync(infoBuilder.ToString());
        }
    }
}
