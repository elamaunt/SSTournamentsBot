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

        readonly IDataService _dataService;

        public RefreshLeaderboardSlashCommand(IDataService dataService)
        {
            _dataService = dataService;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            await ServiceHelpers.RefreshLeaders(context, _dataService, false);
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
