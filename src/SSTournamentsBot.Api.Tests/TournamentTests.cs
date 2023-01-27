using Microsoft.FSharp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSTournamentsBot.Api.Services;
using SSTournamentsBot.Api.Tests.Mocks;
using SSTournamentsBot.Api.Tests.Virtuals;
using System;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Tests
{
    [TestClass]
    public class TournamentTests
    {
        [TestMethod]
        public async Task SubmitGameAfterStageCompletedTest()
        {
            var skia = new SkiaDrawingService();
            var data = new InMemoryDataService();
            var api = new TournamentApi(skia, data);
            var botApi = new VirtualBotApi();
            var context = new Context("test", api, null, botApi, new SetupOptions());

            Assert.IsTrue(data.StoreUsersSteamId(1, 1));
            Assert.IsTrue(data.StoreUsersSteamId(2, 2));
            Assert.IsTrue(data.StoreUsersSteamId(3, 3));
            Assert.IsTrue(data.StoreUsersSteamId(4, 4));

            for (ulong i = 1; i < 5; i++)
            {
                var user = data.FindUserByDiscordId(i);
                Assert.IsTrue((await api.TryRegisterUser(user, await botApi.GetUserName(context, user.DiscordId))) == Domain.RegistrationResult.Registered);
            }

            Assert.IsTrue((await api.TryStartTheCheckIn()).IsDone);

            for (ulong i = 1; i < 5; i++)
                Assert.IsTrue((await api.TryCheckInUser(i)).IsDone);

            Assert.IsTrue(api.IsAllPlayersCheckIned);

            Assert.IsTrue((await api.TryStartTheTournament()).IsDone);

            for (ulong i = 1; i < 5; i++)
            {
                Assert.IsTrue((await api.TryLeaveUser(i, i, TechnicalWinReason.Voting)).IsDone);
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
                Assert.IsTrue((await api.TryLeaveUser(i, i, TechnicalWinReason.OpponentsLeft)).IsDone);
            }

            Assert.IsTrue(api.IsAllActiveMatchesCompleted);
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

        [TestMethod]
        public async Task MatchVotingTest()
        {
            var skia = new SkiaDrawingService();
            var data = new InMemoryDataService();
            var api = new TournamentApi(skia, data);
            var botApi = new VirtualBotApi();
            var context = new Context("test", api, null, botApi, new SetupOptions());

            Assert.IsTrue(data.StoreUsersSteamId(1, 1));
            Assert.IsTrue(data.StoreUsersSteamId(2, 2));
            Assert.IsTrue(data.StoreUsersSteamId(3, 3));
            Assert.IsTrue(data.StoreUsersSteamId(4, 4));

            for (ulong i = 1; i < 5; i++)
            {
                var user = data.FindUserByDiscordId(i);
                Assert.IsTrue((await api.TryRegisterUser(user, await botApi.GetUserName(context, user.DiscordId))) == Domain.RegistrationResult.Registered);
            }

            Assert.IsTrue((await api.TryStartTheCheckIn()).IsDone);

            for (ulong i = 1; i < 5; i++)
                Assert.IsTrue((await api.TryCheckInUser(i)).IsDone);

            Assert.IsTrue(api.IsAllPlayersCheckIned);

            Assert.IsTrue((await api.TryStartTheTournament()).IsDone);

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

                var submitResult = await api.TrySubmitGame(new FinishedGameInfo(winners, losers, gameType, duration, map, usedMod, replayLink));

                Assert.IsTrue(submitResult.IsCompleted || submitResult.IsCompletedAndFinishedTheStage);
            }

            //System.IO.File.WriteAllBytes("sstournamentimage0.png", await api.RenderTournamentImage());

            Assert.IsTrue((await api.TryCompleteCurrentStage()).IsCompleted);
            Assert.IsTrue((await api.TryStartNextStage()).IsDone);

            int votingResult = -1;

            Assert.IsTrue((await api.TryStartVoting(CreateVoting("", 1, true, FSharpFunc<FSharpOption<int>, Unit>.FromConverter(x =>
            {
                votingResult = x.Value;
                return SharedUnit;
            })))).IsCompleted);

            var finalMatch = api.ActiveMatches[0];

            for (ulong i = 1; i < 5; i++)
            {
                if (finalMatch.Player1.Value.Item1.DiscordId == i)
                {
                    Assert.IsTrue((await api.TryAcceptVote(i, 1, GuildRole.Everyone)).IsAccepted);
                }
                else if (finalMatch.Player2.Value.Item1.DiscordId == i)
                {
                    Assert.IsTrue((await api.TryAcceptVote(i, 0, GuildRole.Everyone)).IsAccepted);
                }
                else
                {
                    Assert.IsTrue((await api.TryLeaveUser(i, i, TechnicalWinReason.Voting)).IsDone);
                    Assert.IsTrue((await api.TryAcceptVote(i, 0, GuildRole.Everyone)).IsAccepted);
                }
            }

            Assert.IsTrue((await api.TryCompleteVoting()).IsCompleted);
            Assert.AreEqual(0, votingResult);

            var leaveId = finalMatch.Player1.Value.Item1.DiscordId;
            Assert.IsTrue((await api.TryLeaveUser(leaveId, leaveId, TechnicalWinReason.Voting)).IsDone);
            Assert.IsTrue(api.IsAllActiveMatchesCompleted);
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
            Assert.AreEqual(finalMatch.Player2.Value.Item1.DiscordId, bundle.Winner.Value.DiscordId);

            System.IO.File.WriteAllBytes("sstournamentimage1.png", bundle.Image);
        }

        [TestMethod]
        public async Task TournamentCycleTest()
        {
            var skia = new SkiaDrawingService(); 
            var data = new InMemoryDataService();
            var api = new TournamentApi(skia, data);
            var timeline = new InMemoryEventsTimeline();

            var scanner = new GamesScannerMock();
            var botApi = new VirtualBotApi();

            var options = new VirtualOptions<TournamentEventsOptions>(new TournamentEventsOptions()
            {
                MinimumPlayersToStartCheckin = 4,
                CheckInTimeoutMinutes = 1,
                VotingTimeoutSeconds = 1,
                StageBreakTimeoutMinutes = 1,
                StageTimeoutMinutes = 1,
                AdditionalTimeForStageMinutes = 1
            });

            var contextService = new VirtualContextService();
            var handler = new TournamentEventsHandler(new LoggerMock<TournamentEventsHandler>(), contextService, data, botApi, scanner, timeline, api, options);
            var context = contextService.Context = new Context("test", api, handler, botApi, new SetupOptions());

            // Store users in db
            Assert.IsTrue(data.StoreUsersSteamId(1, 1));
            Assert.IsTrue(data.StoreUsersSteamId(2, 2));
            Assert.IsTrue(data.StoreUsersSteamId(3, 3));
            Assert.IsTrue(data.StoreUsersSteamId(4, 4));

            for (int k = 0; k < 3; k++)
            {
                // Register users in the tournament
                for (ulong i = 1; i < 5; i++)
                {
                    var user = data.FindUserByDiscordId(i);
                    Assert.IsTrue((await api.TryRegisterUser(user, await botApi.GetUserName(context, user.DiscordId))) == Domain.RegistrationResult.Registered);
                }

                timeline.AddOneTimeEventAfterTime(Event.NewStartCheckIn(context.Name), TimeSpan.FromMinutes(options.Value.CheckInTimeoutMinutes));

                // Start checkin
                await GoNextEvent(timeline, handler);

                for (ulong i = 1; i < 5; i++)
                    Assert.IsTrue((await api.TryCheckInUser(i)).IsDone);

                // Start tournament
                await GoNextEvent(timeline, handler);

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

                    var submitResult = await api.TrySubmitGame(new FinishedGameInfo(winners, losers, gameType, duration, map, usedMod, replayLink));

                    Assert.IsTrue(submitResult.IsCompleted || submitResult.IsCompletedAndFinishedTheStage);
                }

                // Complete stage
                await GoNextEvent(timeline, handler);
                // Start new stage
                await GoNextEvent(timeline, handler);

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

                    var submitResult = await api.TrySubmitGame(new FinishedGameInfo(winners, losers, gameType, duration, map, usedMod, replayLink));

                    Assert.IsTrue(submitResult.IsCompleted || submitResult.IsCompletedAndFinishedTheStage);
                }

                // Complete the tournament
                await GoNextEvent(timeline, handler);

                Assert.AreEqual("The tournament is finished normally", botApi.Messages.Last().Message);
            }
        }

        [TestMethod]
        public void TestMapBans()
        {
            var random = new Random(1);
            var p1 = new FSharpOption<Player>(new Player("", 0, 0, RaceOrRandom.RandomEveryMatch, false, MapBans.NoBattleMarshes | MapBans.NoFataMorgana, 0));
            var p2 = new FSharpOption<Player>(new Player("", 0, 0, RaceOrRandom.RandomEveryMatch, false, MapBans.NoMeetingOfMinds | MapBans.NoTranquilitysEnd, 0));

            for (int i = 0; i < 1000; i++)
            {
                var map = GetRandomMapForPlayers(p1, p2, random);

                Assert.AreNotEqual(Map.BattleMarshes, map);
                Assert.AreNotEqual(Map.MeetingOfMinds, map);
                Assert.AreNotEqual(Map.FataMorgana, map);
                Assert.AreNotEqual(Map.TranquilitysEnd, map);
            }
        }

        private static async Task GoNextEvent(InMemoryEventsTimeline timeline, TournamentEventsHandler handler)
        {
            var next = timeline.GetNextEventInfo().Event;
            timeline.RemoveAllEvents();
            await SwitchEvent(next, handler);
        }
    }
}
