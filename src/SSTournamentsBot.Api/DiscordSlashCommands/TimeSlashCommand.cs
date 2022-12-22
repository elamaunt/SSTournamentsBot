using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Threading.Tasks;

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
            var nextEvent = _timeline.GetNextEventInfo();
        }
    }
}
