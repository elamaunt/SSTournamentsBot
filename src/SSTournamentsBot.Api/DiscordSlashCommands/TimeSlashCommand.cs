using Discord.WebSocket;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Helpers;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System;
using System.Globalization;
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

        public override string DescriptionKey=> nameof(S.Commands_Time);

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var isRussian = culture.Name == "ru";
            var nextEvent = _timeline.GetNextEventInfoForContext(context.Name);

            var text = new CompoundText();

            text.AppendLine(OfKey(S.Time_Time).Format(GetUnixTimeStamp(), GetMoscowTime().PrettyShortTimePrint()));

            if (nextEvent != null)
            {
                var e = nextEvent;
                text.AppendLine(OfKey(S.Time_NextEvent).Format(e.Event.PrettyPrint(isRussian), GetTimeBeforeEvent(e).PrettyPrint(isRussian)));
            }
            else
            {
                text.AppendLine(OfKey(S.Bot_NoEvents));
            }

            await arg.RespondAsync(text.Build());
        }
    }
}
