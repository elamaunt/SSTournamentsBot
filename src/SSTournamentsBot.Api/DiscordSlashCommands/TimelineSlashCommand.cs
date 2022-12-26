using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System;
using System.Linq;
using System.Text;
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

            if (events.Length == 0)
            {
                await arg.RespondAsync("Нет запланированных событий.");
                return;
            }

            var builder = new StringBuilder();

            for (int i = 0; i < events.Length; i++)
            {
                var e = events[i];
                builder.AppendLine($"{i + 1}. [{e.Item2.ToString("s")}] {e.Item1} - {GetStringFor(e.Item3)}");
            }

            await arg.RespondAsync(builder.ToString());
        }

        private string GetStringFor(TimeSpan? preiodicTime)
        {
            if (!preiodicTime.HasValue)
                return "одноразовое";

                return $"периодическое через время {preiodicTime.Value}";
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
