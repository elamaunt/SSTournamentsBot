using Discord.WebSocket;
using SSTournamentsBot.Api.Helpers;
using SSTournamentsBot.Api.Services;
using System;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class AddTimeSlashCommand : SlashCommandBase
    {
        readonly IEventsTimeline _timeline;

        public AddTimeSlashCommand(IEventsTimeline timeline)
        {
            _timeline = timeline;
        }

        public override string Name => "add-time";

        public override string Description => "Откладывает следующее событие на +5 минут";

        public override async Task Handle(SocketSlashCommand arg)
        {
            var nextEvent = _timeline.GetNextEventInfo();

            if (nextEvent != null)
            {
                var e = nextEvent;
                _timeline.AddTimeToNextEventWithType(e.Event, TimeSpan.FromMinutes(5));
                await arg.RespondAsync($"Следующее событие '**{e.Event.PrettyPrint()}**' отложено на **5 минут**");
            }
            else
            {
                await arg.RespondAsync("Сейчас нет запланированных событий.");
            }
        }
    }
}
