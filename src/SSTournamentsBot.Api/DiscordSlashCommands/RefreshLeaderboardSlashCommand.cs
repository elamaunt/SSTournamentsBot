using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Helpers;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class RefreshLeaderboardSlashCommand : SlashCommandBase
    {
        public override string Name => "refresh-leaderboard";
        public override string DescriptionKey => nameof(S.Commands_RefreshLeaderboard);

        readonly IBotApi _botApi;
        readonly IDataService _dataService;
        public RefreshLeaderboardSlashCommand(IBotApi botApi, IDataService dataService)
        {
            _botApi = botApi;
            _dataService = dataService;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
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
