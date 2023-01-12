using Discord.WebSocket;
using SSTournamentsBot.Api.Helpers;
using SSTournamentsBot.Api.Services;
using System;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

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

        public override string Description => "Выводит текущее время и оставшееся время до следующего события";

        public override async Task Handle(SocketSlashCommand arg)
        {
            var nextEvent = _timeline.GetNextEventInfo();

            var time = $">>>Ваше время **<t:{GetUnixTimeStamp()}:t>**\nМосковское время **{GetMoscowTime().PrettyShortTimePrint()}**";

            if (nextEvent != null)
            {
                var e = nextEvent;
                await arg.RespondAsync($"{time}\nСледующее событие '**{e.Event.PrettyPrint()}**' наступит через **{GetTimeBeforeEvent(e).PrettyPrint()}**.");
            }
            else
            {
                await arg.RespondAsync($"{time}\nСейчас нет запланированных событий.");
            }
        }
    }
}
