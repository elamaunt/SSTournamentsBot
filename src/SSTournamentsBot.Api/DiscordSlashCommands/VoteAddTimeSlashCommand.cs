using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class VoteAddTimeSlashCommand : SlashCommandBase
    {
        readonly TournamentApi _api;

        public override string Name => "vote-addtime";
        public override string Description => "Начать голосование";

        public VoteAddTimeSlashCommand(TournamentApi api)
        {
            _api = api;
        }

        public override async Task Handle(SocketSlashCommand arg)
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
