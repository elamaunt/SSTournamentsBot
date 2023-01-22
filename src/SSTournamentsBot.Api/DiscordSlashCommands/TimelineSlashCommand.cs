using Discord;
using Discord.WebSocket;
using Microsoft.FSharp.Core;
using SSTournamentsBot.Api.Services;
using System;
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

        public override async Task Handle(Context context, SocketSlashCommand arg)
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
                builder.AppendLine($"{i + 1}. [{e.StartDate.ToString("s")}] {e.Event} - {GetStringFor(e.Period)}");
            }

            await arg.RespondAsync(builder.ToString());
        }

        private string GetStringFor(FSharpOption<TimeSpan> preiodicTime)
        {
            if (!preiodicTime.IsSome())
                return "одноразовое";

                return $"периодическое через каждые {preiodicTime.Value.PrettyPrint()}";
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
