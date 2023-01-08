using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class KickBotSlashCommand : SlashCommandBase
    {
        public override string Name => "kick-bot";
        public override string Description => "Исключить бота из турнира (для тестов)";

        readonly TournamentApi _tournamentApi;
        public KickBotSlashCommand(TournamentApi tournamentApi)
        {
            _tournamentApi = tournamentApi;
        }

        public override async Task Handle(SocketSlashCommand arg)
        {
            var botIdOption = arg.Data.Options.FirstOrDefault(x => x.Name == "botId");
            var reasonOption = arg.Data.Options.FirstOrDefault(x => x.Name == "reason");
            var players = _tournamentApi.RegisteredPlayers;

            var id = (ulong)(long)botIdOption.Value;
            TechnicalWinReason reason;

            switch ((long)reasonOption.Value)
            {
                case 0:
                    reason = TechnicalWinReason.OpponentsLeft;
                    break;
                case 1:
                    reason = TechnicalWinReason.OpponentsKicked;
                    break;
                case 2:
                    reason = TechnicalWinReason.OpponentsBan;
                    break;
                case 3:
                    reason = TechnicalWinReason.Voting;
                    break;
                default:
                    reason = TechnicalWinReason.OpponentsLeft;
                    break;

            }

            if (await _tournamentApi.TryLeaveUser(id, id, reason))
                await arg.RespondAsync("Бот исключен.");
            else
                await arg.RespondAsync("Не удалось исключить бота.");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("botId")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer)
                    .WithDescription("Id пользователя"))
                    
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("reason")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer)
                    .WithDescription("Id причины"))
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
