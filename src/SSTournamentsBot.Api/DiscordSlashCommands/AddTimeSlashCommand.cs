using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Helpers;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System;
using System.Globalization;
using System.Linq;
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

        public override string DescriptionKey=> nameof(S.Commands_AddTime);

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var isRussian = culture.Name == "ru";
            var minutesOption = arg.Data.Options.FirstOrDefault(x => x.Name == "minutes");
            var minutes = (long)minutesOption.Value;

            if (minutes == 0)
            {
                await arg.RespondAsync(OfKey(nameof(S.AddTime_NoMinutes)).Format(arg.CommandName).Build(culture));
                return;
            }

            var nextEvent = _timeline.GetNextEventInfo();

            if (nextEvent != null)
            {
                var time = TimeSpan.FromMinutes(minutes);
                var e = nextEvent;
                _timeline.AddTimeToNextEventWithType(e.Event, time);

                if (minutes < 0)
                    await arg.RespondAsync(OfKey(nameof(S.AddTime_NextEventSpeedUp)).Format(e.Event.PrettyPrint(isRussian), time.Negate().PrettyPrint(isRussian)).Build(culture));
                else
                    await arg.RespondAsync(OfKey(nameof(S.AddTime_NextEventDelayed)).Format(e.Event.PrettyPrint(isRussian), time.PrettyPrint(isRussian)).Build(culture));
            }
            else
            {
                await arg.RespondAsync(OfKey(nameof(S.AddTime_NoEvents)).Build(culture));
            }
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("minutes")
                    .WithDescription("Время в минутах")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer))
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
