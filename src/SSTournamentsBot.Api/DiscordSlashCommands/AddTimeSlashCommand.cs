using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Helpers;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System;
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

        public override string Description => "Откладывает или ускоряет следующее событие (для админов)";

        public override async Task Handle(Context context, SocketSlashCommand arg)
        {
            var minutesOption = arg.Data.Options.FirstOrDefault(x => x.Name == "minutes");
            var minutes = (long)minutesOption.Value;

            if (minutes == 0)
            {
                await arg.RespondAsync(OfKey(nameof(S.AddTime_NoMinutes)).Format(arg.CommandName).Build());
                return;
            }

            var nextEvent = _timeline.GetNextEventInfo();

            if (nextEvent != null)
            {
                var time = TimeSpan.FromMinutes(minutes);
                var e = nextEvent;
                _timeline.AddTimeToNextEventWithType(e.Event, time);

                if (minutes < 0)
                    await arg.RespondAsync($"> Следующее событие '**{e.Event.PrettyPrint()}**' ускорено на -**{time.Negate().PrettyPrint()}**");
                else
                    await arg.RespondAsync($"> Следующее событие '**{e.Event.PrettyPrint()}**' отложено на **{time.PrettyPrint()}**");
            }
            else
            {
                await arg.RespondAsync("> Сейчас нет запланированных событий.");
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
