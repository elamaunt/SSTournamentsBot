﻿using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System;
using System.Globalization;
using System.Threading.Tasks;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class ForceBotsMatchesSlashCommand : SlashCommandBase
    {
        public override string Name => "force-bots-matches";
        public override string DescriptionKey=> nameof(S.Commands_ForceBotMatches);

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var random = new Random();
            var matches = context.TournamentApi.ActiveMatches;

            foreach (var match in matches)
            {
                var p1 = match.Player1.Value.Item1;
                var p2 = match.Player2.Value.Item1;
                if (random.Next(2) == 0 && p1.IsBot)
                {
                    await context.TournamentApi.TryLeaveUser(p1.DiscordId, p1.SteamId, TechnicalWinReason.OpponentsLeft);
                } else if (p2.IsBot)
                {
                    await context.TournamentApi.TryLeaveUser(p2.DiscordId, p2.SteamId, TechnicalWinReason.OpponentsLeft);
                }
                else if (p1.IsBot)
                {
                    await context.TournamentApi.TryLeaveUser(p1.DiscordId, p1.SteamId, TechnicalWinReason.OpponentsLeft);
                }
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
