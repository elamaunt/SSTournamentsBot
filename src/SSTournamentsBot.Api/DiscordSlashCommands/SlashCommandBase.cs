using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Services;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public abstract class SlashCommandBase
    {
        public abstract string Name { get; }
        public abstract string DescriptionKey { get; }
        public string Description => OfKey(DescriptionKey).Build(CultureInfo.GetCultureInfo("ru"));

        public abstract Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture);
       
        public SlashCommandBuilder MakeBuilder()
        {
            var ru = OfKey(DescriptionKey).Build(CultureInfo.GetCultureInfo("ru"));
            var en = OfKey(DescriptionKey).Build(CultureInfo.GetCultureInfo("en"));

            var builder = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(ru)
                .WithDescriptionLocalizations(new Dictionary<string, string>() 
                {
                    { "ru", ru },
                    { "ru-RU", ru },
                    { "en", en },
                    { "en-EN", en }
                });
            Configure(builder);
            return builder;
        }

        protected virtual void Configure(SlashCommandBuilder builder) { }

        protected Text OfKey(string key) => Text.OfKey(key);
    }
}
