using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using SSTournamentsBot.Api.Helpers;
using SSTournamentsBot.Api.Services;
using System.Text;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class RefreshLeaderboardSlashCommand : SlashCommandBase
    {
        public override string Name => "refresh-leaderboard";
        public override string Description => "Обновить таблицу лидеров (для админов)";

        readonly IBotApi _botApi;
        readonly IDataService _dataService;
        public RefreshLeaderboardSlashCommand(IBotApi botApi, IDataService dataService)
        {
            _botApi = botApi;
            _dataService = dataService;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg)
        {
            await ServiceHelpers.RefreshLeaders(context, _botApi, _dataService, false);
            await arg.RespondAsync("Таблица лидеров обновлена.");
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
