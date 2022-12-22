using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class TimelineSlashCommand : SlashCommandBase
    {
        readonly IEventsTimeline _timeline;

        public TimelineSlashCommand(IEventsTimeline timeline)
        {
            _timeline = timeline;
        }

        public override string Name => "timeline";

        public override string Description => "Вывести раписание всех запланированных событий";

        public override async Task Handle(SocketSlashCommand arg)
        {
            var events = _timeline.GetAllScheduledEvents();
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder
                .WithDefaultPermission(true)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
