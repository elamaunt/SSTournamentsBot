﻿using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class SubmitGameSlashCommand : SlashCommandBase
    {
        public override string Name => "submit-game";
        public override string DescriptionKey=> nameof(S.Commands_SubmitGame);

        readonly IEventsTimeline _timeline;

        public SubmitGameSlashCommand(IEventsTimeline timeline)
        {
            _timeline = timeline;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var botIdOption = arg.Data.Options.FirstOrDefault(x => x.Name == "winner-id");
            var id = (ulong)(long)botIdOption.Value;

            var match = context.TournamentApi.ActiveMatches.FirstOrDefault(x => x.Player1.ValueOrDefault()?.Item1.DiscordId == id || x.Player2.ValueOrDefault()?.Item1.DiscordId == id);

            if (match == null)
            {
                await arg.RespondAsync("Нет активного матча с указанным пользовательским ID");
                return;
            }

            var isPlayerHas1SameId = match.Player1.ValueOrDefault()?.Item1.DiscordId == id;
            var winner = isPlayerHas1SameId ? match.Player1.ValueOrDefault() : match.Player2.ValueOrDefault();
            var loser = isPlayerHas1SameId ? match.Player2.ValueOrDefault() : match.Player1.ValueOrDefault();

            var winners = new Tuple<ulong, RaceInfo>[] { new Tuple<ulong, RaceInfo>(winner.Item1.SteamId, RaceInfo.NewNormalRace(winner.Item2)) };
            var losers = new Tuple<ulong, RaceInfo>[] { new Tuple<ulong, RaceInfo>(loser.Item1.SteamId, RaceInfo.NewNormalRace(loser.Item2)) };

            var gameType = GameType.Type1v1;
            var duration = 100;
            var map = MapInfo.NewMap1v1(match.Map, match.Map.ToString());
            var usedMod = ModInfo.NewMod(Mod.Soulstorm);
            var replayLink = "";

            var result = await context.TournamentApi.TrySubmitGame(new FinishedGameInfo(winners, losers, gameType, duration, map, usedMod, replayLink));

            if (result.IsCompleted || result.IsCompletedAndFinishedTheStage)
            {
                await arg.RespondAsync("Игра засчитана");

                if (result.IsCompletedAndFinishedTheStage)
                {
                    _timeline.AddOneTimeEventAfterTime(context.Name, Event.NewCompleteStage(context.Name), TimeSpan.FromSeconds(10));
                }
            }
            else
                await arg.RespondAsync($"Не удалось засчитать игру: {result}");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("winner-id")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer)
                    .WithDescription("Id победителя"))
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
