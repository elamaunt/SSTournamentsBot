using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class ForceEventSlashCommand : SlashCommandBase
    {
        readonly IEventsTimeline _timeline;
        readonly ITournamentEventsHandler _handler;

        public ForceEventSlashCommand(IEventsTimeline timeline, ITournamentEventsHandler handler)
        {
            _timeline = timeline;
            _handler = handler;
        }

        public override string Name => "force-event";

        public override string Description => "Форсирует следующее событие (для админов)";

        public override async Task Handle(SocketSlashCommand arg)
        {
            var nextEvent = _timeline.GetNextEventInfo();

            if (nextEvent != null)
            {
                _timeline.RemoveEventInfo(nextEvent);
                await arg.RespondAsync($"> Следующее событие было формировано.");
                await SwitchEvent(nextEvent.Event, _handler);
            }
            else
            {
                await arg.RespondAsync($"> Сейчас нет запланированных событий.");
            }
        }
    }
}
