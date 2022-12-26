using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class VoteSlashCommand : SlashCommandBase
    {
        public override string Name => "vote";

        public override string Description => "Начать голосование";

        public override Task Handle(SocketSlashCommand arg)
        {
            throw new NotImplementedException();
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder
              .AddOption(new SlashCommandOptionBuilder()
                  .WithName("type")
                  .WithDescription("Тип вопроса")
                      .AddChoice("kick", 0)
                      .AddChoice("ban", 1)
                      .AddChoice("add-time", 2)
                  .WithType(ApplicationCommandOptionType.Integer)
                  .WithRequired(true));
        }
    }
}
