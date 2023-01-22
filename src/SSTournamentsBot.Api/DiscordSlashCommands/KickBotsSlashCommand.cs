using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Threading.Tasks;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class KickBotsSlashCommand : SlashCommandBase
    {
        public override string Name => "kick-bots";
        public override string Description => "Исключить всех ботов из турнира (для тестов)";

        readonly TournamentApi _tournamentApi;
        public KickBotsSlashCommand(TournamentApi tournamentApi)
        {
            _tournamentApi = tournamentApi;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg)
        {
            var players = _tournamentApi.RegisteredPlayers;

            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p.IsBot)
                    await _tournamentApi.TryLeaveUser(p.DiscordId, p.SteamId, TechnicalWinReason.OpponentsLeft);
            }

            await arg.RespondAsync("Все боты покинули игру.");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
