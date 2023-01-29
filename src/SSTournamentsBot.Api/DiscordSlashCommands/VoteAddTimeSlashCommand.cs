using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class VoteAddTimeSlashCommand : SlashCommandBase
    {
        public override string Name => "vote-addtime";
        public override string DescriptionKey=> nameof(S.Commands_VoteAddTime);

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var type = (long?)arg.Data.Options.FirstOrDefault(x => x.Name == "type")?.Value ?? null;

            // context.TournamentApi.TryStartVoting(Voting.)
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
