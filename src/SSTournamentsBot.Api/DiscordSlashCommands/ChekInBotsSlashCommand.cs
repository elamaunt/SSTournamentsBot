using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class ChekInBotsSlashCommand : SlashCommandBase
    {
        public override string Name => "checkin-bots";
        public override string DescriptionKey=> nameof(S.Commands_CheckInBots);

        readonly TournamentApi _tournamentApi;
        public ChekInBotsSlashCommand(TournamentApi tournamentApi)
        {
            _tournamentApi = tournamentApi;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var players = _tournamentApi.RegisteredPlayers;
            var count = (long?)arg.Data.Options.FirstOrDefault(x => x.Name == "count")?.Value ?? null;

            await arg.DeferAsync();

            if (!count.HasValue)
                for (int i = 0; i < players.Length; i++)
                {
                    var p = players[i];
                    if (p.IsBot)
                        await _tournamentApi.TryCheckInUser(p.SteamId);
                }
            else
                for (int i = 0; i < count.Value; i++)
                {
                    var p = players.Where(x => x.IsBot).ElementAtOrDefault(i);
                    if (p?.IsBot ?? false)
                        await _tournamentApi.TryCheckInUser(p.SteamId);
                }

            await arg.ModifyOriginalResponseAsync(x => x.Content = "Все боты зачекинелись.");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("count")
                    .WithDescription("Количество ботов")
                    .WithRequired(false)
                    .WithType(ApplicationCommandOptionType.Integer))
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
