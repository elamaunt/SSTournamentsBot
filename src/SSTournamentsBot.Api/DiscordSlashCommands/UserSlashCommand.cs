using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class UserSlashCommand : SlashCommandBase
    {
        public override string Name => "user";
        public override string DescriptionKey=> nameof(S.Commands_User);

        readonly IDataService _dataService;
        public UserSlashCommand(IDataService dataService)
        {
            _dataService = dataService;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var userOption = arg.Data.Options.First(x => x.Name == "user");
            var user = (IUser)userOption.Value;
            var userData = _dataService.FindUserByDiscordId(user.Id);

            if (userData == null)
            {
                await arg.RespondAsync("Пользователь не зарегистрирован в системе.");
                return;
            }

            var builder = new StringBuilder();
            
            builder.AppendLine($"Name: **{user.Username}**");
            builder.AppendLine($"SteamId: **{userData.SteamId}**");
            builder.AppendLine($"DiscordId: **{userData.DiscordId}**");
            builder.AppendLine($"VotingDisabled: **{userData.VotingDisabled}**");
            builder.AppendLine($"StatsVerified: **{userData.StatsVerified}**");
            builder.AppendLine($"Penalties: **{userData.Penalties}**");
            builder.AppendLine($"Score: **{userData.Score}**");
            builder.AppendLine($"Race: **{userData.Race}**");
            builder.AppendLine($"HasLowPriority: **{userData.HasLowPriority}**");
            builder.AppendLine($"Banned: **{userData.Banned}**");
            builder.AppendLine($"MapBans: **{userData.Map1v1Bans}**");

            if (builder.Length > 0)
                await arg.RespondAsync(builder.ToString());
            else
                await arg.RespondAsync("Нет данных.");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder
                .WithDefaultPermission(true)
                 .AddOption(new SlashCommandOptionBuilder()
                    .WithName("user")
                    .WithDescription("Пользователь")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.User))
                .WithDefaultMemberPermissions(GuildPermission.Administrator | GuildPermission.ModerateMembers)
                .WithDMPermission(true);
        }
    }
}
