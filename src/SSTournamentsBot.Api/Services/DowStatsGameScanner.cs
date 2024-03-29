﻿using HttpBuilder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Resources;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class DowStatsGameScanner : IGameScanner
    {
        readonly DowStatsGameScannerOptions _options;
        readonly ILogger<DowStatsGameScanner> _logger;
        private readonly IContextService _contextService;
        readonly HttpService _httpService;
        readonly IEventsTimeline _timeline;
        readonly ConcurrentDictionary<string, string> _submitedGames = new ConcurrentDictionary<string, string>();

        volatile int _listenersCount;
        DateTime _lastScan;
        Timer _rescanTimer;

        readonly ConcurrentDictionary<string, Context> _listeningContexts = new ConcurrentDictionary<string, Context>();

        public DowStatsGameScanner(
            ILogger<DowStatsGameScanner> logger, 
            IContextService contextService,
            HttpService httpService,
            IEventsTimeline timeline,
            IOptions<DowStatsGameScannerOptions> options)
        {
            _options = options.Value;
            _logger = logger;
            _contextService = contextService;
            _httpService = httpService;
            _timeline = timeline;
        }


        public void StartForContext(Context context)
        {
            if (_listeningContexts.TryAdd(context.Name, context))
            {
                if (Interlocked.Increment(ref _listenersCount) > 0)
                {
                    StartScan();
                }
            }
        }

        public void StopForContext(Context context)
        {
            if (_listeningContexts.TryRemove(context.Name, out var value))
            {
                if (Interlocked.Decrement(ref _listenersCount) == 0)
                {
                    StopScan();
                }
            }
        }

        private void StartScan()
        {
            _submitedGames.Clear();
            _lastScan = GetMoscowTime();
            _rescanTimer = new Timer(OnReScan, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(_options.ReScanPeriod));
        }

        private void OnReScan(object state)
        {
            var fromDate = _lastScan;
            var toDate = GetMoscowTime();

            _httpService.Build("https://dowstats.ru/api/lastgames.php", UriKind.Absolute)
                .WithParameter("datetime_from", fromDate.AddMilliseconds(-_options.ReScanPeriod / 2).ToString("s", DateTimeFormatInfo.InvariantInfo))
                .WithParameter("datetime_to", toDate.AddMilliseconds(_options.ReScanPeriod / 2).ToString("s", DateTimeFormatInfo.InvariantInfo))
                .Get()
                .Send()
                .ValidateSuccessStatusCode()
                .Json<DowStatsGameDTO[]>(JsonSerializer.CreateDefault())
                .Task.ContinueWith(async t =>
                {
                    var mainContext = _contextService.GetMainContext();
                    if (t.IsFaulted)
                    {
                        await Log(mainContext, "Error on the DowStats lastgames request. " + t.Exception);
                        return;
                    }

                    _lastScan = toDate;

                    var games = t.Result;

                    for (int i = 0; i < games.Length; i++)
                    {
                        try
                        {
                            var game = games[i];

                            if (game.server != "steam")
                                continue;

                            var winnersSelection = game.players.Where(x => x.result == "win");
                            var losersSelection = game.players.Where(x => x.result == "los");
                            var winners = winnersSelection.Select(x => Tuple.Create(ulong.Parse(x.sid), ResolveRace(x.race))).ToArray();
                            var losers = losersSelection.Select(x => Tuple.Create(ulong.Parse(x.sid), ResolveRace(x.race))).ToArray();

                            var gameType = ResolveGameType(winners.Length, losers.Length);

                            var info = new FinishedGameInfo(
                                winners,
                                losers,
                                gameType,
                                int.Parse(game.matchDurationSeconds),
                                ResolveMapInfo(game.map),
                                ResolveModInfo(game.modification),
                                game.gameLink);

                            var playersString = $"{game.modification} | {game.registerTime} | {string.Join(", ", winnersSelection.Select(x => x.name))} VS {string.Join(", ", losersSelection.Select(x => x.name))}";

                            await Log(mainContext, $"Trying to submit a game globally: {playersString}");

                            if (_submitedGames.TryAdd(playersString, playersString))
                            {
                                foreach (var context in _contextService.AllContexts)
                                {
                                    await Log(context, $"Trying to submit a game to context: {playersString}");

                                    var contectRef = context;

                                    var handled = await contectRef.TournamentApi.TrySubmitGame(info)
                                        .ContinueWith(async submitTask =>
                                        {
                                            if (submitTask.IsFaulted)
                                            {
                                                await Log(contectRef, "Error on submitting the gameInfo. " + submitTask.Exception);
                                                return true;
                                            }
                                             
                                            if (submitTask.IsCompleted)
                                            {
                                                var result = submitTask.Result;
                                                await Log(contectRef, "Game submitted");


                                                if (result.IsCompleted || result.IsCompletedAndFinishedTheStage)
                                                {
                                                    await context.BotApi.SendMessage(context, Text.OfKey(nameof(S.Scanner_MatchAccepted)).Format(string.Join(", ", winnersSelection.Select(x => x.name)), string.Join(", ", losersSelection.Select(x => x.name)), game.gameLink), GuildThread.EventsTape | GuildThread.TournamentChat);
                                                    await Log(contectRef, $"The game is counted");
                                                }
                                                else
                                                {
                                                    await Log(contectRef, $"The game is not counted due to reason: {result}");
                                                }

                                                if (result.IsCompletedAndFinishedTheStage)
                                                {
                                                    _timeline.AddOneTimeEventAfterTime(contectRef.Name, Event.NewCompleteStage(contectRef.Name), TimeSpan.FromSeconds(10));
                                                }

                                                return result.IsCompleted || result.IsCompletedAndFinishedTheStage;
                                            }

                                            return false;
                                        }).Unwrap();

                                    if (handled)
                                        break;
                                }
                            }
                            else
                            {
                                await Log(mainContext, $"The game is already submitted: {playersString}");
                            }
                        }
                        catch (Exception ex)
                        {
                            await Log(mainContext, "Error on processing of the gameInfo. " + ex);
                        }
                    }
                });
        }

        private RaceInfo ResolveRace(string race)
        {
            switch (race)
            {
                case "Eldar": return RaceInfo.NewNormalRace(Race.Eldar);
                case "Necrons": return RaceInfo.NewNormalRace(Race.Necrons);
                case "Space Marines": return RaceInfo.NewNormalRace(Race.SpaceMarines);
                case "Chaos Marines": return RaceInfo.NewNormalRace(Race.Chaos);
                case "Orks": return RaceInfo.NewNormalRace(Race.Orks);
                case "Sisters of Battle": return RaceInfo.NewNormalRace(Race.SisterOfBattle);
                case "Imperial Guard": return RaceInfo.NewNormalRace(Race.ImperialGuard);
                case "Tau Empire": return RaceInfo.NewNormalRace(Race.Tau);
                case "Dark Eldar": return RaceInfo.NewNormalRace(Race.DarkEldar);
                default: return RaceInfo.NewModRace(race);
            }
        }

        private ModInfo ResolveModInfo(string modification)
        {
            if (modification == "dxp2" || modification == "dxp2 ")
                return ModInfo.NewMod(Mod.Soulstorm);

            if (modification == "tournamentpatch")
                return ModInfo.NewMod(Mod.TPMod);

            return ModInfo.NewModName(modification);
        }

        private MapInfo ResolveMapInfo(string map)
        {
            switch (map.ToLowerInvariant())
            {
                case "2p_blood_river":
                case "2p_blood_river_[rem]": return MapInfo.NewMap1v1(Map.BloodRiver, map);

                case "2p_outer_reaches": return MapInfo.NewMap1v1(Map.OuterReaches, map);

                case "2p_shrine_of_excellion":
                case "2p_shrine_of_excellion_[rem]": return MapInfo.NewMap1v1(Map.ShrineOfExcellion, map);

                case "2p_titan_fall":
                case "2p_titan_fall_[rem]": return MapInfo.NewMap1v1(Map.TitansFall, map);

                case "2p_tranquilitys_end":
                case "2p_tranquilitys_end_[rem]": return MapInfo.NewMap1v1(Map.TranquilitysEnd, map);

                case "2p_fata_morgana":
                case "2p_fata_morgana_[rem]": return MapInfo.NewMap1v1(Map.FataMorgana, map);

                case "2p_fallen_city":
                case "2p_fallen_city_[rem]": return MapInfo.NewMap1v1(Map.FallenCity, map);

                case "2p_quests_triumph": return MapInfo.NewMap1v1(Map.QuestsTriumph, map);

                case "2p_battle_marshes": return MapInfo.NewMap1v1(Map.BattleMarshes, map);

                case "2p_deadly_fun_archeology": return MapInfo.NewMap1v1(Map.DeadlyFunArcheology, map);

                case "2p_sugaroasis": return MapInfo.NewMap1v1(Map.SugarOasis, map);

                case "2p_meeting_of_minds": return MapInfo.NewMap1v1(Map.MeetingOfMinds, map);
                default: return MapInfo.NewMapName(map);
            }
        }

        private GameType ResolveGameType(int winners, int losers)
        {
            if (winners == 1 && losers == 1)
                return GameType.Type1v1;
            if (winners == 2 && losers == 2)
                return GameType.Type2v2;
            if (winners == 3 && losers == 3)
                return GameType.Type3v3;
            if (winners == 4 && losers == 4)
                return GameType.Type4v4;

            return GameType.TypeUnspecified;
        }

        private void StopScan()
        {
            _rescanTimer?.Change(Timeout.Infinite, 0);
        }

        private Task Log(Context context, string message)
        {
            _logger.LogInformation(message);
            return context.BotApi.Log(context, message);
        }

        public class DowStatsGameDTO
        {
            public DowStatsGamePlayer[] players { get; set; }
            public string server { get; set; }
            public string withBanUser { get; set; }
            public string modification { get; set; }
            public string matchDurationSeconds { get; set; }
            public string map { get; set; }
            public string replayDownloadLink { get; set; }
            public string gameLink { get; set; }
            public string registerTime { get; set; }
            public string id { get; set; }
        }

        public class DowStatsGamePlayer
        {
            public string sid { get; set; }
            public int mmr { get; set; }
            public string name { get; set; }
            public string race { get; set; }
            public string result { get; set; }
            public int newMmr { get; set; }
        }
    }
}