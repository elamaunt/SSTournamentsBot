﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSTournamentsBot.Api.Services;
using SSTournamentsBot.Api.Services.Debug;
using SSTournamentsBot.Api.Tests.Mocks;
using SSTournamentsBot.Api.Tests.Virtuals;
using System;
using System.Threading.Tasks;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.Tests
{
    [TestClass]
    public class TournamentTests
    {
        [TestMethod]
        public async Task SubmitGameAfterStageCompletedTest()
        {
            var skia = new SkiaDrawingService();
            var api = new TournamentApi(skia);

            //var timeline = new InMemoryEventsTimeline();
            var data = new InMemoryDataService();

            //var scanner = new GamesScannerMock();
            var botApi = new VirtualBotApi();

            //var options = new VirtualOptions<TournamentEventsOptions>(new TournamentEventsOptions() { });

            //var handler = new TournamentEventsHandler(new LoggerMock<TournamentEventsHandler>(), data, botApi, scanner, timeline, api, options);
            //var timeScheduler = new TimeSchedulerService(new LoggerMock<TimeSchedulerService>(), handler, timeline);

            try
            {
                //await timeScheduler.StartAsync(CancellationToken.None);

                Assert.IsTrue(data.StoreUsersSteamId(1, 1));
                Assert.IsTrue(data.StoreUsersSteamId(2, 2));
                Assert.IsTrue(data.StoreUsersSteamId(3, 3));
                Assert.IsTrue(data.StoreUsersSteamId(4, 4));

                for (ulong i = 1; i < 5; i++)
                {
                    var user = data.FindUserByDiscordId(i);
                    Assert.IsTrue((await api.TryRegisterUser(user, await botApi.GetUserName(user.DiscordId))) == Domain.RegistrationResult.Ok);
                }

                Assert.IsTrue((await api.TryStartTheCheckIn()).IsDone);

                for (ulong i = 1; i < 5; i++)
                    Assert.IsTrue((await api.TryCheckInUser(i)).IsDone);

                Assert.IsTrue(api.IsAllPlayersCheckIned());

                Assert.IsTrue((await api.TryStartTheTournament()).IsDone);

                for (ulong i = 1; i < 5; i++)
                {
                    Assert.IsTrue(await api.TryLeaveUser(i, i, TechnicalWinReason.Voting));
                }

                Assert.IsTrue((await api.TryCompleteCurrentStage()).IsCompleted);

                foreach (var match in api.ActiveMatches)
                {
                    var winner = match.Player1.ValueOrDefault();
                    var loser = match.Player2.ValueOrDefault();

                    var winners = new Tuple<ulong, RaceInfo>[] { new Tuple<ulong, RaceInfo>(winner.Item1.SteamId, RaceInfo.NewNormalRace(winner.Item2)) };
                    var losers = new Tuple<ulong, RaceInfo>[] { new Tuple<ulong, RaceInfo>(loser.Item1.SteamId, RaceInfo.NewNormalRace(loser.Item2)) };

                    var gameType = GameType.Type1v1;
                    var duration = 100;
                    var map = MapInfo.NewMap1v1(match.Map, match.Map.ToString());
                    var usedMod = ModInfo.NewMod(Mod.Soulstorm);
                    var replayLink = "";

                    Assert.IsTrue((await api.TrySubmitGame(new FinishedGameInfo(winners, losers, gameType, duration, map, usedMod, replayLink))).IsCompleted);
                }

                Assert.IsTrue((await api.TryStartNextStage()).IsDone);

                for (ulong i = 1; i < 5; i++)
                {
                    Assert.IsTrue(await api.TryLeaveUser(i, i, TechnicalWinReason.OpponentsLeft));
                }

                Assert.IsTrue(api.IsAllActiveMatchesCompleted());
                Assert.IsTrue((await api.TryCompleteCurrentStage()).IsCompleted);
                Assert.IsTrue((await api.TryStartNextStage()).IsTheStageIsTerminal);

                var bundle = await api.BuildAllData();
                Assert.IsNotNull(bundle);
                await api.DropTournament();

                Assert.IsTrue((await api.TryStartTheTournament()).IsNoTournament);

                Assert.IsNotNull(bundle.PlayedMatches);
                Assert.AreEqual(3, bundle.PlayedMatches.Length);
                Assert.IsNotNull(bundle.Image);
                Assert.IsTrue(bundle.Image.Length > 0);
                Assert.IsNotNull(bundle.Winner);
            }
            finally
            {
                //await timeScheduler.StopAsync(CancellationToken.None);
            }
        }
    }
}
