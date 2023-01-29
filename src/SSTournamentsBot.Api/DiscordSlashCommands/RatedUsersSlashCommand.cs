using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class RatedUsersSlashCommand : SlashCommandBase
    {
        public override string Name => "rated-users";
        public override string DescriptionKey=> nameof(S.Commands_RatedUsers);

        readonly IBotApi _botApi;
        readonly IDataService _dataService;
        public RatedUsersSlashCommand(IBotApi botApi, IDataService dataService)
        {
            _botApi = botApi;
            _dataService = dataService;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var builder = new StringBuilder();

            var users = _dataService.LoadAllsUsersWithScore();

            for (int i = 0; i < users.Length; i++)
            {
                var user = users[i];
                builder.AppendLine($"{i + 1}. {user.Score} - **{await _botApi.GetUserName(context, user.DiscordId)}**");
            }

            if (builder.Length > 0)
                await arg.RespondAsync(builder.ToString());
            else
                await arg.RespondAsync("Нет данных.");
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
