﻿using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class MatchesSlashCommand : SlashCommandBase
    {
        public override string Name => "matches";

        public override string DescriptionKey=> nameof(S.Commands_Matches);

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var builder = new StringBuilder();

            var matches = context.TournamentApi.ActiveMatches;

            for (int i = 0; i < matches.Length; i++)
            {
                var m = matches[i];

                var p1 = m.Player1.ValueOrDefault();
                var p2 = m.Player2.ValueOrDefault();
                builder.AppendLine($"{m.Id}. {p1?.Item1.Name} | {p1?.Item2}  VS  {p2?.Item1.Name} | {p2?.Item2} / {m.Map} / {m.BestOf} / {m.Result}");
            }

            if (builder.Length > 0)
                await arg.RespondAsync(builder.ToString());
            else
                await arg.RespondAsync("В данный момент нет активных матчей");
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
