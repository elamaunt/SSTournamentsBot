using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class VoteBanSlashCommand : SlashCommandBase
    {
        readonly TournamentApi _api;

        public override string Name => "vote-ban";
        public override string DescriptionKey=> nameof(S.Commands_VoteBan);

        public VoteBanSlashCommand(TournamentApi api)
        {
            _api = api;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var type = (long?)arg.Data.Options.FirstOrDefault(x => x.Name == "type")?.Value ?? null;

            // _api.TryStartVoting(Voting.)
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder
              .AddOption(new SlashCommandOptionBuilder()
                  /*.WithName("type")
                  .WithDescription("Тип вопроса")
                      .AddChoice("kick", 0)
                      .AddChoice("ban", 1)
                      .AddChoice("add-time", 2)
                  .WithType(ApplicationCommandOptionType.Integer)
                  .WithRequired(true)*/);
        }
    }
}
