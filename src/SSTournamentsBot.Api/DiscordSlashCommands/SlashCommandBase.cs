using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Services;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public abstract class SlashCommandBase
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract Task Handle(Context context, SocketSlashCommand arg);
       
        public SlashCommandBuilder MakeBuilder()
        {
            var builder = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description);
            Configure(builder);
            return builder;
        }

        protected virtual void Configure(SlashCommandBuilder builder) { }

        protected Text OfKey(string key) => Text.OfKey(key);
    }
}
