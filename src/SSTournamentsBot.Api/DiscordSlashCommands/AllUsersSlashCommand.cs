using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Text;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class AllUsersSlashCommand : SlashCommandBase
    {
        public override string Name => "all-users";
        public override string Description => "Вывести список пользователей в бд (для админов)";

        readonly IBotApi _botApi;
        readonly IDataService _dataService;
        public AllUsersSlashCommand(IBotApi botApi, IDataService dataService)
        {
            _botApi = botApi;
            _dataService = dataService;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg)
        {
            var builder = new StringBuilder();

            int i = 1;

            foreach (var user in _dataService.EnumerateAllUsers())
            {
                builder.AppendLine($"{i++}. {user.Score} | {user.Penalties} | {user.SteamId} | {user.DiscordId} | **{await _botApi.GetUserName(context, user.DiscordId)}**");
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
