using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class ForceEventSlashCommand : SlashCommandBase
    {
        readonly IEventsTimeline _timeline;

        public ForceEventSlashCommand(IEventsTimeline timeline)
        {
            _timeline = timeline;
        }

        public override string Name => "force-event";

        public override string DescriptionKey=> nameof(S.Commands_ForceEvent);

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var nextEvent = _timeline.GetNextEventInfoForContext(context.Name);

            if (nextEvent != null)
            {
                _timeline.RemoveEventInfo(nextEvent);
                await arg.RespondAsync(OfKey(nameof(S.ForceEvent_Done)).Build(culture));
                await SwitchEvent(nextEvent.Event, context.EventsHandler);
            }
            else
            {
                await arg.RespondAsync(OfKey(nameof(S.ForceEvent_NoEvents)).Build(culture));
            }
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
