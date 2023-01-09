using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FSharp.Core;
using SSTournamentsBot.Api.DataDomain;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class TournamentEventsHandler : ITournamentEventsHandler
    {
        readonly ILogger<TournamentEventsHandler> _logger;
        readonly IDataService _dataService;
        readonly IBotApi _botApi;
        readonly IGameScanner _scanner;
        readonly IEventsTimeline _timeline;
        readonly TournamentEventsOptions _options;
        readonly TournamentApi _tournamentApi;

        IButtonsController _activeVotingButtons;
        IButtonsController ActiveVotingButtons
        {
            get => _activeVotingButtons;
            set
            {
                var oldValue = Interlocked.CompareExchange(ref _activeVotingButtons, value, _activeVotingButtons);
                oldValue?.DisableButtons("Голосование завершено.");
            }
        }

        ulong[] Mentions => _tournamentApi.RegisteredPlayers.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray();

        public TournamentEventsHandler(ILogger<TournamentEventsHandler> logger,
            IDataService dataService,
            IBotApi botApi,
            IGameScanner scanner,
            IEventsTimeline timeline,
            TournamentApi tournamentApi,
            IOptions<TournamentEventsOptions> options)
        {
            _logger = logger;
            _dataService = dataService;
            _botApi = botApi;
            _scanner = scanner;
            _timeline = timeline;
            _options = options.Value;
            _tournamentApi = tournamentApi;
        }

        public async void DoCompleteStage()
        {
            try
            {
                await Log("An attempt to complete the stage..");
                _timeline.RemoveAllEventsWithType(Event.CompleteStage);

                var result = await _tournamentApi.TryCompleteCurrentStage();

                if (result.IsNoTournament)
                {
                    await Log("No a planned tournament");
                    return;
                }

                if (result.IsNoUncompletedStage)
                {
                    await Log("No an uncompleted stage");
                    return;
                }

                if (result.IsNotAllMatchesFinished)
                {
                    var matches = _tournamentApi.ActiveMatches;

                    var countForNotCompleted = matches.Count(x => x.Result.IsNotCompleted);

                    if (matches.Length == 1 && !_tournamentApi.SingleMatchTimeAlreadyExtended)
                    {
                        _tournamentApi.SingleMatchTimeAlreadyExtended = true;
                        await _botApi.SendMessage("> Единственный матч в этой стадии еще не доигран. Завершение стадии отложено на __**5 минут.**__", GuildThread.EventsTape | GuildThread.TournamentChat);
                        _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromMinutes(_options.AdditionalTimeForStageMinutes));
                        return;
                    }

                    if (countForNotCompleted >= matches.Length / 2 && !_tournamentApi.TimeAlreadyExtended)
                    {
                        _tournamentApi.TimeAlreadyExtended = true;
                        await _botApi.SendMessage("> Половина или более матчей еще не доиграны. Завершение стадии отложено на __**5 минут**__.", GuildThread.EventsTape | GuildThread.TournamentChat);
                        _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromMinutes(_options.AdditionalTimeForStageMinutes));
                        return;
                    }

                    var activeMatch = matches.FirstOrDefault(x => x.Result.IsNotCompleted);

                    if (activeMatch != null)
                    {
                        await Log("Not all matches finished");

                        var p1 = activeMatch.Player1.Value.Item1;
                        var p2 = activeMatch.Player2.Value.Item1;

                        var voting = CreateVoting(">>> Быстрое голосование по незавершенному матчу. Кому присудить техническое поражение?",
                            2,
                            true,
                            FSharpFunc<FSharpOption<int>, Unit>.FromConverter(x =>
                            {
                                Task.Run(async () =>
                                {
                                    var sameMatch = _tournamentApi.ActiveMatches.FirstOrDefault(X => X.Id == activeMatch.Id);

                                    if (sameMatch == null || !sameMatch.Result.IsNotCompleted)
                                    {
                                        await _botApi.SendMessage("> Результат голосования не принят, так как матч был завершен.", GuildThread.EventsTape | GuildThread.TournamentChat);
                                        return;
                                    }

                                    if (x.IsNone())
                                    {
                                        await _botApi.SendMessage(">>> Результат голосования не принят, так как голоса разделились поровну, либо никто не проголосовал.\nНо решение все равно нужно принять, поэтому техническое поражение выдается игроку на усмотрение бота.", GuildThread.EventsTape | GuildThread.TournamentChat);
                                        await Task.Delay(2000);
                                        x = new FSharpOption<int>(new Random().NextDouble() >= 0.5 ? 0 : 1);
                                    }

                                    switch (x.Value)
                                    {
                                        case 0:
                                            await _tournamentApi.TryLeaveUser(p1.DiscordId, p1.SteamId, TechnicalWinReason.Voting);
                                            await _botApi.SendMessage($"> Присуждено техническое поражение игроку под ником **{p1.Name}**.", GuildThread.EventsTape | GuildThread.TournamentChat, p1.DiscordId);
                                            break;
                                        case 1:
                                            await _tournamentApi.TryLeaveUser(p2.DiscordId, p2.SteamId, TechnicalWinReason.Voting);
                                            await _botApi.SendMessage($"> Присуждено техническое поражение игроку под ником **{p2.Name}**.", GuildThread.EventsTape | GuildThread.TournamentChat, p2.DiscordId);
                                            break;
                                        default:
                                            break;
                                    }

                                    if (_tournamentApi.IsAllActiveMatchesCompleted())
                                    {
                                        _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromSeconds(10));
                                    }
                                });

                                return SharedUnit;
                            }));

                        voting = AddVoteOption(voting, p1.Name, BotButtonStyle.Danger);
                        voting = AddVoteOption(voting, p2.Name, BotButtonStyle.Danger);

                        var startVotingResult = await _tournamentApi.TryStartVoting(voting);

                        if (startVotingResult.IsCompleted)
                        {
                            ActiveVotingButtons = await _botApi.SendVotingButtons(voting.Message, voting.Options, GuildThread.VotingsTape | GuildThread.TournamentChat, Mentions);
                            _timeline.AddOneTimeEventAfterTime(Event.CompleteVoting, TimeSpan.FromSeconds(_options.VotingTimeoutSeconds));
                        }

                        _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromSeconds(_options.VotingTimeoutSeconds + 10));
                        return;
                    }
                    else
                    {
                        _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromSeconds(10));
                        return;
                    }
                }

                if (result.IsCompleted)
                {
                    await Log("The stage has been completed");

                    if (_tournamentApi.PossibleNextStageMatches == 0)
                    {
                        DoStartNextStage();
                    }
                    else
                    {
                        await _botApi.SendMessage($"Стадия была успешно завершена! Следующая стадия начнется после {_options.StageBreakTimeoutMinutes}-минутного перерыва.", GuildThread.EventsTape | GuildThread.TournamentChat);

                        _timeline.AddOneTimeEventAfterTime(Event.StartNextStage, TimeSpan.FromMinutes(_options.StageBreakTimeoutMinutes));
                    }
                    return;
                }

                await Log("Broken state");
            }
            catch (Exception ex)
            {
                await Log(nameof(DoCompleteStage) + ':' + ex.ToString());
            }
        }

        public async void DoStartCheckIn()
        {
            try
            {
                await Log("An attempt to start checkin stage..");

                var result = await _tournamentApi.TryStartTheCheckIn();

                if (result.IsNoTournament)
                {
                    await Log("No a planned tournament");
                    await _botApi.SendMessage("Без турниров сегодня :)", GuildThread.EventsTape | GuildThread.TournamentChat);
                    return;
                }

                if (result.IsAlreadyStarted)
                {
                    await Log("Tournament is running");
                    return;
                }

                if (result.IsNotEnoughPlayers)
                {
                    await Log("Not enough players.");
                    await _tournamentApi.DropTournament();
                    await _botApi.SendMessage("Турнир отменен, так как участников недостаточно. Список зарегистрированных был обнулен.\nСледующий турнир только завтра :)", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
                    await PrintTimeAndNextEvent();
                    return;
                }

                if (result.IsDone)
                {
                    await Log("Checkin stage starting..");

                    await _botApi.SendMessage("Внимание! Началась стадия чекина на турнир.\nВсем участникам нужно выполнить команду __**/checkin**__ на турнирном канале для подтверждения своего участия.\nДлительность чек-ина 15 минут.\nЕсли все игроки зачекинятся до окончания времени, то турнир начнется немедленно.\nКто не успел зарегистрироваться, самое время.", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);

                    _timeline.AddOneTimeEventAfterTime(Event.StartCurrentTournament, TimeSpan.FromMinutes(_options.CheckInTimeoutMinutes));
                    return;
                }

                await Log("Broken state");
            }
            catch (Exception ex)
            {
                await Log(nameof(DoStartCheckIn) + ':' + ex.ToString());
            }
        }

        public async void DoStartCurrentTournament()
        {
            try
            {
                _logger.LogInformation("An attempt to start the tournament..");
                _timeline.RemoveAllEventsWithType(Event.StartCurrentTournament);

                var result = await _tournamentApi.TryStartTheTournament();

                if (result.IsNoTournament)
                {
                    await Log("No a planned tournament");
                    return;
                }

                if (result.IsAlreadyStarted)
                {
                    await Log("Tournament is running");
                    return;
                }

                if (result.IsDone)
                {
                    await Log("Starting the tournament..");

                    await _botApi.SendMessage("Начинается турнир!", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
                    await _botApi.SendFile(await _tournamentApi.RenderTournamentImage(), "tournament.png", "Сетка:", GuildThread.EventsTape | GuildThread.TournamentChat);
                    _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromMinutes(_options.StageTimeoutMinutes));
                    _scanner.GameTypeFilter = GameType.Type1v1;
                    _scanner.Active = true;

                    await PrintMatches();
                    await _botApi.SendMessage($"Первая стадия турнира началась. Пошел отсчет времени __**{TimeSpan.FromMinutes(_options.StageTimeoutMinutes).PrettyPrint()}**__ до конца стадии.", GuildThread.EventsTape | GuildThread.TournamentChat);

                    return;
                }

                if (result.IsNotEnoughPlayers)
                {
                    var players = _tournamentApi.RegisteredPlayers;
                    await Log("Not enough players");
                    await _tournamentApi.DropTournament();
                    await _botApi.SendMessage("В данный момент турнир невозможно начать, так как участников недостаточно.\nМинимальное количество участников 4.\nСписок зарегистрированных был обнулен.", GuildThread.EventsTape | GuildThread.TournamentChat, players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());
                    return;
                }

                await Log("Broken state");
            }
            catch (Exception ex)
            {
                await Log(nameof(DoStartCurrentTournament) + ':' + ex.ToString());
            }
        }

        private async Task PrintMatches(bool remindRules = true)
        {
            var builder = new StringBuilder();
            var matches = _tournamentApi.ActiveMatches;

            builder.AppendLine(">>> **Матчи, которые должны быть сыграны в текущей стадии турнира:**");
            builder.AppendLine();

            for (int i = 0; i < matches.Length; i++)
            {
                var m = matches[i];

                var p1 = m.Player1.Value;
                var p2 = m.Player2.Value;

                var p1Mention = p1.Item1.IsBot ? p1.Item1.Name : await _botApi.GetMention(p1.Item1.DiscordId);
                var p2Mention = p2.Item1.IsBot ? p2.Item1.Name : await _botApi.GetMention(p2.Item1.DiscordId);

                builder.AppendLine($"{m.Id + 1}. {p1Mention} {p2Mention} | {m.Map}");
                builder.AppendLine($"**{p1.Item1.Name}** ({p1.Item2})  VS  **{p2.Item1.Name}** ({p2.Item2})");
                builder.AppendLine();
            }

            if (remindRules)
            {
                builder.AppendLine("**Не забывайте использовать DowStats и прикрепленный Steam аккаунт, иначе матч не будет засчитан!**");
                builder.AppendLine();
            }

            await _botApi.SendMessage(builder.ToString(), GuildThread.EventsTape | GuildThread.TournamentChat);

            var hostingMentions = Mentions.Where(x =>
            {
                return matches.Any(m => m.Player1.Value.Item1.DiscordId == x);
            }).ToArray();

            await _botApi.SendMessage("---\nИгру хостят вот эти ребята:", GuildThread.EventsTape | GuildThread.TournamentChat);
            await _botApi.Mention(GuildThread.EventsTape | GuildThread.TournamentChat, hostingMentions);

            var relaxingMentions = Mentions.Where(x =>
            {
                return !matches.Any(m => m.Player1.Value.Item1.DiscordId == x || m.Player2.Value.Item1.DiscordId == x);
            }).ToArray();

            if (relaxingMentions.Length > 0)
            {
                await _botApi.SendMessage("---\nНа скамейке отдыхающих на этот раз:", GuildThread.EventsTape | GuildThread.TournamentChat);
                await _botApi.Mention(GuildThread.EventsTape | GuildThread.TournamentChat, relaxingMentions);
            }
        }

        public async void DoStartNextStage()
        {
            try
            {
                await Log("An attempt to start the next stage..");

                var result = await _tournamentApi.TryStartNextStage();

                while (result.IsDone && (await _tournamentApi.TryCompleteCurrentStage()) == CompleteStageResult.Completed)
                    result = await _tournamentApi.TryStartNextStage();

                if (result.IsNoTournament)
                {
                    await Log("No a planned tournament");
                    return;
                }

                if (result.IsPreviousStageIsNotCompleted)
                {
                    await Log("The previous stage is not completed");
                    return;
                }

                if (result.IsTheStageIsTerminal)
                {
                    _scanner.Active = false;

                    await Log("The stage is terminal");

                    if (!_tournamentApi.PlayedMatches.Any(x => x.Result.IsWinner))
                    {
                        await _botApi.SendMessage("Определился победитель, но турнир не будет засчитан, так как он полностью состоит из технических поражений. Должна быть сыграна хотя бы одна игра.", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
                        await _botApi.SendMessage($"__**Daily Tournament {_tournamentApi.Date.PrettyShortDatePrint()} завершен без учета результатов**__\n==================================================================================================\n", GuildThread.EventsTape | GuildThread.TournamentChat);
                        await _tournamentApi.DropTournament();
                        await Task.Delay(5000);
                        await PrintTimeAndNextEvent();
                        await Log("The tournament is finished without results");
                        return;
                    }

                    await _botApi.SendMessage("Определился победитель турнира!", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);

                    var tournamentBundle = await _tournamentApi.BuildAllData();
                    var date = _tournamentApi.Date;
                    await _botApi.SendFile(tournamentBundle.Image, "tournament.png", "Полная сетка:", GuildThread.EventsTape | GuildThread.TournamentChat);
                    await UploadTournamentToHistory(tournamentBundle);
                    await UpdateLeaderboardAndUploadChangesToHistory(tournamentBundle);
                    await _botApi.SendMessage($"__**Daily Tournament {date.PrettyShortDatePrint()} успешно завершен**__\n==================================================================================================\n", GuildThread.EventsTape | GuildThread.TournamentChat);
                    await _tournamentApi.DropTournament();
                    await Task.Delay(2000);
                    await PrintTimeAndNextEvent();
                    await Log("The tournament is finished normally");
                    return;
                }

                if (result.IsDone)
                {
                    await Log("The stage has been started..");
                    await _botApi.SendMessage(">>> Начинается следующая стадия турнира! Генерация сетки..", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
                    await _botApi.SendFile(await _tournamentApi.RenderTournamentImage(), "tournament.png", "Сетка турнира", GuildThread.EventsTape | GuildThread.TournamentChat);

                    _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromMinutes(_options.StageTimeoutMinutes));
                    _scanner.GameTypeFilter = GameType.Type1v1;
                    _scanner.Active = true;

                    await PrintMatches(false);
                    await _botApi.SendMessage($"Новая стадия турнира началась. Пошел отсчет времени __**{TimeSpan.FromMinutes(_options.StageTimeoutMinutes).PrettyPrint()}**__ до конца стадии.", GuildThread.EventsTape | GuildThread.TournamentChat);
                    return;
                }

                await Log("Broken state");
            }
            catch (Exception ex)
            {
                await Log(nameof(DoStartNextStage) + ':' + ex.ToString());
            }
        }

        public async void DoStartPreCheckingTimeVote()
        {
            try
            {
                await Log("An attempt to start the pre checking time vote..");

                var voting = CreateVoting($">>> @here *До начала чек-ина в турнире осталось __**{_options.PreCheckinTimeVotingOffsetMinutes} минут**__.*\nСтоит ли отложить чекин турнира и его начало на **30 минут**?\n\nГолосовать могут только участники, зарегистрированные на следующий турнир, и администрация сервера.",
                            1,
                            false,
                            FSharpFunc<FSharpOption<int>, Unit>.FromConverter(x =>
                            {
                                Task.Run(async () =>
                                {
                                    if (x.IsNone())
                                    {
                                        await _botApi.SendMessage(">>> Результат голосования не принят, так как голосов было недостаточно, либо голоса разделились поровну.\nТурнир начнется в запланированное время.", GuildThread.EventsTape | GuildThread.TournamentChat);
                                        await PrintTimeAndNextEvent();
                                        return;
                                    }

                                    switch (x.Value)
                                    {
                                        case 0:
                                            _timeline.AddTimeToNextEventWithType(Event.StartCheckIn, TimeSpan.FromMinutes(30));
                                            await _botApi.SendMessage($"> Начало турнира и чекин отложены на 30 минут.", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
                                            break;
                                        case 1:
                                            await _botApi.SendMessage($"> Начало турнира отложено не будет и начнется уже скоро.", GuildThread.EventsTape | GuildThread.TournamentChat);
                                            break;
                                        default:
                                            break;
                                    }

                                    await PrintTimeAndNextEvent();
                                });

                                return SharedUnit;
                            }));

                voting = AddVoteOption(voting, "+30 минут к началу турнира и чекину", BotButtonStyle.Success);
                voting = AddVoteOption(voting, "Не добавлять время", BotButtonStyle.Secondary);

                var startVotingResult = await _tournamentApi.TryStartVoting(voting);

                if (startVotingResult.IsCompleted)
                {
                    await Log("The PreCheckingTime voting has been started..");

                    var players = _tournamentApi.RegisteredPlayers;

                    ActiveVotingButtons = await _botApi.SendVotingButtons(voting.Message, voting.Options, GuildThread.VotingsTape | GuildThread.TournamentChat, Mentions);
                    _timeline.AddOneTimeEventAfterTime(Event.CompleteVoting, TimeSpan.FromMinutes(_options.PreCheckinTimeVotingOffsetMinutes - 1));
                    _timeline.AddOneTimeEventAfterTime(Event.StartCheckIn, TimeSpan.FromMinutes(_options.PreCheckinTimeVotingOffsetMinutes));
                    return;
                }

                await Log("Broken state");
            }
            catch (Exception ex)
            {
                await Log(nameof(DoStartPreCheckingTimeVote) + ':' + ex.ToString());
            }
        }

        public async void DoCompleteVoting()
        {
            try
            {
                _timeline.RemoveAllEventsWithType(Event.CompleteVoting);
                ActiveVotingButtons = null;
                await Log("An attempt to complete the voting..");

                var result = await _tournamentApi.TryCompleteVoting();

                if (result.IsCompleted)
                {
                    await Log("The voting is completed");
                    return;
                }

                if (result.IsCompletedWithNoEnoughVotes)
                {
                    await Log("The voting has no enough votes");
                    return;
                }

                if (result.IsNoVoting)
                {
                    await Log("There is no voting now");
                    return;
                }

                if (result.IsTheVoteIsOver)
                {
                    await Log("The voting is over");
                    return;
                }

                await Log("Broken state");
            }
            catch (Exception ex)
            {
                await Log(nameof(DoCompleteVoting) + ':' + ex.ToString());
            }
        }

        private async Task UpdateLeaderboardAndUploadChangesToHistory(TournamentBundle bundle)
        {
            await Log("Updating leaderboards");
            var modifiedUsers = new Dictionary<ulong, (UserData Data, string Name, int AddedScore, int Penalties)>();

            (UserData Data, string Name, int AddedScore, int Penalties) userInfo;

            for (int i = 0; i < bundle.PlayedMatches.Length; i++)
            {
                var match = bundle.PlayedMatches[i];

                if (match.Result.IsWinner)
                {
                    var matchWinner = ((MatchResult.Winner)match.Result).Item1;

                    if (matchWinner.IsBot)
                        continue;

                    if (!modifiedUsers.TryGetValue(matchWinner.SteamId, out userInfo))
                    {
                        var data = _dataService.FindUserByDiscordId(matchWinner.DiscordId);
                        userInfo = modifiedUsers[matchWinner.SteamId] = (data, matchWinner.Name, 0, data.Penalties);
                    }

                    var newAddedScore = userInfo.AddedScore == 0 ? 10 : userInfo.AddedScore * 2;
                    modifiedUsers[matchWinner.SteamId] = (userInfo.Data, userInfo.Name, newAddedScore, Math.Max(0, userInfo.Penalties - 1));
                    continue;
                }

                if (match.Result.IsTechnicalWinner)
                {
                    var matchWinner = ((MatchResult.TechnicalWinner)match.Result).Item1;
                    var loser = match.Player1.Value.Item1.SteamId == matchWinner.SteamId ? match.Player2.Value.Item1 : match.Player1.Value.Item1;

                    if (loser.IsBot)
                        continue;

                    if (!modifiedUsers.TryGetValue(loser.SteamId, out userInfo))
                    {
                        var data = _dataService.FindUserByDiscordId(loser.DiscordId);
                        userInfo = modifiedUsers[loser.SteamId] = (data, loser.Name, 0, data.Penalties);
                    }

                    modifiedUsers[loser.SteamId] = (userInfo.Data, userInfo.Name, userInfo.AddedScore, Math.Max(0, userInfo.Penalties + 3));
                    continue;
                }
            }

            var tournamentWinner = bundle.Winner.ValueOrDefault();

            if (tournamentWinner != null && modifiedUsers.TryGetValue(tournamentWinner.SteamId, out userInfo))
            {
                modifiedUsers[tournamentWinner.SteamId] = (userInfo.Data, userInfo.Name, userInfo.AddedScore / 2 * 3, userInfo.Penalties);
            }

            var (printedChanges, mentions) = PrintChangesAndUpdateUsersInDataService(modifiedUsers);
            await ServiceHelpers.RefreshLeaders(_botApi, _dataService);

            if (printedChanges != null && mentions != null)
                await _botApi.SendMessage(printedChanges, GuildThread.EventsTape | GuildThread.TournamentChat | GuildThread.History, mentions);
        }

        private (string, ulong[]) PrintChangesAndUpdateUsersInDataService(Dictionary<ulong, (UserData Data, string Name, int AddedScore, int Penalties)> modifiedUsers)
        {
            if (modifiedUsers.Values.Count == 0)
                return (null, null);

            var builder = new StringBuilder();

            builder.AppendLine("--- __**Изменения в рейтинге**__ ---");
            builder.AppendLine();

            foreach (var info in modifiedUsers.Values.OrderByDescending(x => x.AddedScore))
            {
                var data = info.Data;
                data.Score = data.Score + info.AddedScore;
                data.Penalties = info.Penalties;

                if (!_dataService.UpdateUser(data))
                    Log($"WARNING! Unable to update the users rating. User Steamid = {data.SteamId}. Rating = {data.Score}. Penalties = {data.Penalties}");
            }

            var mentionsList = new List<ulong>();

            int i = 1;
            foreach (var info in modifiedUsers.Values.OrderByDescending(x => x.AddedScore))
            {
                if (info.AddedScore != 0)
                {
                    mentionsList.Add(info.Data.DiscordId);
                    builder.AppendLine($"{i++}. {info.AddedScore} | {info.Name}");
                }
            }

            return (builder.ToString(), mentionsList.ToArray());
        }

        private async Task UploadTournamentToHistory(TournamentBundle bundle)
        {
            await Log("Uploading the tournament to history");
            var data = new TournamentData()
            { 
                Date = bundle.Tournament.Date,
                Type = bundle.Tournament.Type,
                Mod = bundle.Tournament.Mod,
                Seed = bundle.Tournament.Seed,
                WinnerSteamId = bundle.Winner.ValueOrDefault()?.SteamId,
                PlayersSteamIds = bundle.Tournament.RegisteredPlayers.Select(x => x.SteamId).ToArray(),
                Matches = bundle.PlayedMatches.Select(x => 
                {
                    return new MatchData() 
                    {
                        Result = x.Result,
                        Replays = x.Replays.ToArray(),
                        PlayerSteamId1 = x.Player1.Value.Item1.SteamId,
                        PlayerSteamId2 = x.Player2.Value.Item1.SteamId
                    };
                }).ToArray()

            };

            _dataService.StoreTournament(data);
            await _botApi.SendMessage($"__**Daily Tournament {bundle.Tournament.Date.PrettyShortDatePrint()}**__ благополучно завершен!\nПоздравляем с победой игрока под ником __**{bundle.Winner.Value.Name}**__. Всем остальным желаем удачи на следующих турнирах!", GuildThread.History, Mentions);
            await _botApi.SendFile(bundle.Image, "tournament.png", "Сетка:", GuildThread.History);

            var builder = new StringBuilder();

            builder.AppendLine(">>> Сыгранные матчи:");
            builder.AppendLine();

            int k = 0;
            for (int i = 0; i < bundle.PlayedMatches.Length; i++)
            {
                var match = bundle.PlayedMatches[i];

                if (match.Result.IsWinner)
                {
                    builder.AppendLine($"{k + 1}. **{match.Player1.Value.Item1.Name}** ({match.Player1.Value.Item2}) VS **{match.Player2.Value.Item1.Name}** ({match.Player2.Value.Item2}) | {match.Map}");

                    for (int j = 0; j < match.Replays.Length; j++)
                        builder.AppendLine($"{ match.Replays[j].Url}");

                    builder.AppendLine();
                    k++;
                }
            }

            builder.AppendLine();
            await _botApi.SendMessage(builder.ToString(), GuildThread.History);
        }

        private async Task PrintTimeAndNextEvent()
        {
            var nextEvent = _timeline.GetNextEventInfo();

            if (nextEvent != null)
            {
                var e = nextEvent;
                await _botApi.SendMessage($"Московское время {GetMoscowTime().PrettyShortDateAndTimePrint()}\nСледующее событие '**{e.Event.PrettyPrint()}**' наступит через **{GetTimeBeforeEvent(e).PrettyPrint()}**.", GuildThread.TournamentChat | GuildThread.EventsTape);
            }
        }

        private Task Log(string message)
        {
            _logger.LogInformation(message);
            return _botApi.Log(message);
        }
    }
}
