using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Helpers;
using SSTournamentsBot.Api.Services;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class RefreshLeaderboardV2SlashCommand : SlashCommandBase
    {
        public override string Name => "refresh-leaderboard-v2";
        public override string Description => "Обновить таблицу лидеров V2 (для админов)";

        readonly IBotApi _botApi;
        readonly IDataService _dataService;
        public RefreshLeaderboardV2SlashCommand(IBotApi botApi, IDataService dataService)
        {
            _botApi = botApi;
            _dataService = dataService;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg)
        {
            await ServiceHelpers.RefreshLeadersV2(context, _botApi, _dataService, false);
            await arg.RespondAsync("Таблица лидеров обновлена (V2).");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder
                .WithDefaultPermission(true)
                .WithDefaultMemberPermissions(GuildPermission.Administrator | GuildPermission.ModerateMembers)
                .WithDMPermission(true);
        }
    }
}
