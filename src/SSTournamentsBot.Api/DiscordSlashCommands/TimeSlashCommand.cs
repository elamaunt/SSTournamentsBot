using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System;
using System.Threading.Tasks;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class TimeSlashCommand : SlashCommandBase
    {
        readonly IEventsTimeline _timeline;

        public TimeSlashCommand(IEventsTimeline timeline)
        {
            _timeline = timeline;
        }

        public override string Name => "time";

        public override string Description => "Выводит текущее московское время и оставшееся время до следующего события";

        public override async Task Handle(SocketSlashCommand arg)
        {
            var moscowTime = GetMoscowTime();
            var nextEvent = _timeline.GetNextEventInfo();

            if (nextEvent.HasValue)
            {
                var e = nextEvent.Value;
                await arg.RespondAsync($"Московское время {moscowTime}\nСледующее событие {e.Item1} наступит через {(e.Date + (e.Period ?? TimeSpan.Zero)) - moscowTime}");
            }
            else
            {
                await arg.RespondAsync("Сейчас нет запланированных событий.");
            }
        }
    }
}
