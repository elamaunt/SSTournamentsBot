using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class DeleteContextDataSlashCommand : SlashCommandBase
    {
        readonly IDataService _dataService;

        public DeleteContextDataSlashCommand(IDataService dataService)
        {
            _dataService = dataService;
        }

        public override string Name => "delete-context-data";

        public override string DescriptionKey=> nameof(S.Commands_DeleteContextData);

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            _dataService.DeleteAllUserInActivity(context.Name);
            await arg.RespondAsync("Данные контекста обнулены.");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
