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
        readonly IContextService _contextService;
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

        ulong[] Mentions => _tournamentApi.ActivePlayers.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray();
        ulong[] AllPlayersMentions => _tournamentApi.RegisteredPlayers.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray();

        public TournamentEventsHandler(ILogger<TournamentEventsHandler> logger,
            IContextService contextService,
            IDataService dataService,
            IBotApi botApi,
            IGameScanner scanner,
            IEventsTimeline timeline,
            TournamentApi tournamentApi,
            IOptions<TournamentEventsOptions> options)
        {
            _logger = logger;
            _contextService = contextService;
            _dataService = dataService;
            _botApi = botApi;
            _scanner = scanner;
            _timeline = timeline;
            _options = options.Value;
            _tournamentApi = tournamentApi;
        }

        public async Task DoCompleteStage(string contextName)
        {
            var context = GetContextByName(contextName);

            try
            {
                await Log(context, "An attempt to complete the stage..");
                _timeline.RemoveAllEventsWithType(Event.NewCompleteStage(contextName));

                var result = await _tournamentApi.TryCompleteCurrentStage();

                if (result.IsNoTournament)
                {
                    await Log(context, "No a planned tournament");
                    return;
                }

                if (result.IsNoUncompletedStage)
                {
                    await Log(context, "No an uncompleted stage");
                    return;
                }

                if (result.IsNotAllMatchesFinished)
                {
                    var matches = _tournamentApi.ActiveMatches;

                    var countForNotCompleted = matches.Count(x => x.Result.IsNotCompleted);

                    if (matches.Length == 1 && !_tournamentApi.SingleMatchTimeAlreadyExtended)
                    {
                        _tournamentApi.SingleMatchTimeAlreadyExtended = true;
                        await _botApi.SendMessage(context, "> Единственный матч в этой стадии еще не доигран. Завершение стадии отложено на __**5 минут.**__", GuildThread.EventsTape | GuildThread.TournamentChat);
                        _timeline.AddOneTimeEventAfterTime(Event.NewCompleteStage(contextName), TimeSpan.FromMinutes(_options.AdditionalTimeForStageMinutes));
                        return;
                    }

                    if (countForNotCompleted >= matches.Length / 2 && !_tournamentApi.TimeAlreadyExtended)
                    {
                        _tournamentApi.TimeAlreadyExtended = true;
                        await _botApi.SendMessage(context, "> Половина или более матчей еще не доиграны. Завершение стадии отложено на __**5 минут**__.", GuildThread.EventsTape | GuildThread.TournamentChat);
                        _timeline.AddOneTimeEventAfterTime(Event.NewCompleteStage(contextName), TimeSpan.FromMinutes(_options.AdditionalTimeForStageMinutes));
                        return;
                    }

                    var activeMatch = matches.FirstOrDefault(x => x.Result.IsNotCompleted);

                    if (activeMatch != null)
                    {
                        await Log(context, "Not all matches finished");

                        var p1 = activeMatch.Player1.Value.Item1;
                        var p2 = activeMatch.Player2.Value.Item1;

                        var voting = CreateVoting(">>> Быстрое голосование по незавершенному матчу. Кому присудить техническое поражение?",
                            1,
                            true,
                            FSharpFunc<FSharpOption<int>, Unit>.FromConverter(x =>
                            {
                                Task.Run(async () =>
                                {
                                    var sameMatch = _tournamentApi.ActiveMatches.FirstOrDefault(X => X.Id == activeMatch.Id);

                                    if (sameMatch == null || !sameMatch.Result.IsNotCompleted)
                                    {
                                        await _botApi.SendMessage(context, "> Результат голосования не принят, так как матч был завершен.", GuildThread.EventsTape | GuildThread.TournamentChat);
                                        return;
                                    }

                                    if (x.IsNone())
                                    {
                                        await _botApi.SendMessage(context, ">>> Результат голосования не принят, так как голоса разделились поровну, либо никто не проголосовал.\nНо решение все равно нужно принять, поэтому техническое поражение выдается игроку на усмотрение бота.", GuildThread.EventsTape | GuildThread.TournamentChat);
                                        await Task.Delay(2000);
                                        x = new FSharpOption<int>(new Random().NextDouble() >= 0.5 ? 0 : 1);
                                    }

                                    switch (x.Value)
                                    {
                                        case 0:
                                            await _tournamentApi.TryLeaveUser(p1.DiscordId, p1.SteamId, TechnicalWinReason.Voting);
                                            await _botApi.SendMessage(context, $"> Присуждено техническое поражение игроку под ником **{p1.Name}**.", GuildThread.EventsTape | GuildThread.TournamentChat, p1.DiscordId);
                                            break;
                                        case 1:
                                            await _tournamentApi.TryLeaveUser(p2.DiscordId, p2.SteamId, TechnicalWinReason.Voting);
                                            await _botApi.SendMessage(context, $"> Присуждено техническое поражение игроку под ником **{p2.Name}**.", GuildThread.EventsTape | GuildThread.TournamentChat, p2.DiscordId);
                                            break;
                                        default:
                                            break;
                                    }

                                    if (_tournamentApi.IsAllActiveMatchesCompleted)
                                    {
                                        _timeline.AddOneTimeEventAfterTime(Event.NewCompleteStage(contextName), TimeSpan.FromSeconds(10));
                                    }
                                });

                                return SharedUnit;
                            }));

                        voting = AddVoteOption(voting, p1.Name, BotButtonStyle.Danger);
                        voting = AddVoteOption(voting, p2.Name, BotButtonStyle.Danger);

                        var startVotingResult = await _tournamentApi.TryStartVoting(voting);

                        if (startVotingResult.IsCompleted)
                        {
                            ActiveVotingButtons = await _botApi.SendVotingButtons(context, voting.Message, voting.Options, GuildThread.VotingsTape | GuildThread.TournamentChat, Mentions);
                            _timeline.AddOneTimeEventAfterTime(Event.NewCompleteVoting(contextName), TimeSpan.FromSeconds(_options.VotingTimeoutSeconds));
                        }

                        _timeline.AddOneTimeEventAfterTime(Event.NewCompleteStage(contextName), TimeSpan.FromSeconds(_options.VotingTimeoutSeconds + 10));
                        return;
                    }
                    else
                    {
                        _timeline.AddOneTimeEventAfterTime(Event.NewCompleteStage(contextName), TimeSpan.FromSeconds(10));
                        return;
                    }
                }

                if (result.IsCompleted)
                {
                    await Log(context, "The stage has been completed");

                    if (_tournamentApi.PossibleNextStageMatches == 0)
                    {
                        await DoStartNextStage(contextName);
                    }
                    else
                    {
                        await _botApi.SendMessage(context, $"Стадия была успешно завершена! Следующая стадия начнется после {_options.StageBreakTimeoutMinutes}-минутного перерыва.", GuildThread.EventsTape | GuildThread.TournamentChat);

                        _timeline.AddOneTimeEventAfterTime(Event.NewStartNextStage(contextName), TimeSpan.FromMinutes(_options.StageBreakTimeoutMinutes));
                    }
                    return;
                }

                await Log(context, "Broken state");
            }
            catch (Exception ex)
            {
                await Log(context, nameof(DoCompleteStage) + ':' + ex.ToString());
            }
        }

        public async Task DoStartCheckIn(string contextName)
        {
            var context = GetContextByName(contextName);

            try
            {
                await Log(context, "An attempt to start checkin stage..");

                var result = await _tournamentApi.TryStartTheCheckIn();

                if (result.IsNoTournament)
                {
                    await Log(context, "No a planned tournament");
                    await _botApi.SendMessage(context, "Без турниров сегодня :)", GuildThread.EventsTape | GuildThread.TournamentChat);
                    return;
                }

                if (result.IsAlreadyStarted)
                {
                    await Log(context, "Tournament is running");
                    return;
                }

                if (result.IsNotEnoughPlayers)
                {
                    await Log(context, "Not enough players.");
                    await _tournamentApi.DropTournament();
                    await _botApi.SendMessage(context, $"Турнир отменен, так как участников недостаточно. Список зарегистрированных был обнулен.\nСледующий турнир начнется, когда снова зарегистрируются {_options.MinimumPlayersToStartCheckin} участника.", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
                    return;
                }

                if (result.IsDone)
                {
                    await Log(context, "Checkin stage starting..");

                    await _botApi.MentionWaitingRole(context, GuildThread.EventsTape | GuildThread.TournamentChat);
                    await _botApi.SendMessage(context, $"Внимание! Началась стадия чекина на турнир *{_tournamentApi.TournamentType} AutoCup {_tournamentApi.Id}*.\nВсем участникам нужно выполнить команду __**/checkin**__ на турнирном канале для подтверждения своего участия.\nДлительность чек-ина {_options.CheckInTimeoutMinutes} минут.\nРегистрация открыта до окончания чекина, при этом новым участникам вызов команды */checkin* не требуется, достаточно команды */play*", GuildThread.EventsTape | GuildThread.TournamentChat, AllPlayersMentions);

                    _timeline.AddOneTimeEventAfterTime(Event.NewStartCurrentTournament(contextName), TimeSpan.FromMinutes(_options.CheckInTimeoutMinutes));
                    return;
                }

                await Log(context, "Broken state");
            }
            catch (Exception ex)
            {
                await Log(context, nameof(DoStartCheckIn) + ':' + ex.ToString());
            }
        }

        public async Task DoStartCurrentTournament(string contextName)
        {
            var context = GetContextByName(contextName);

            try
            {
                _logger.LogInformation("An attempt to start the tournament..");
                _timeline.RemoveAllEventsWithType(Event.NewStartCurrentTournament(contextName));

                var result = await _tournamentApi.TryStartTheTournament();

                if (result.IsNoTournament)
                {
                    await Log(context, "No a planned tournament");
                    return;
                }

                if (result.IsAlreadyStarted)
                {
                    await Log(context, "Tournament is running");
                    return;
                }

                if (result.IsDone)
                {
                    await Log(context, "Starting the tournament..");

                    await _botApi.SendMessage(context, $"Начинается турнир *{_tournamentApi.TournamentType} AutoCup {_tournamentApi.Id}*!", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
                    await _botApi.SendFile(context, await _tournamentApi.RenderTournamentImage(), $"tournament_{_tournamentApi.Id}.png", "Сетка:", GuildThread.EventsTape | GuildThread.TournamentChat);
                    _timeline.AddOneTimeEventAfterTime(Event.NewCompleteStage(contextName), TimeSpan.FromMinutes(_options.StageTimeoutMinutes));
                    _scanner.GameTypeFilter = GameType.Type1v1;
                    _scanner.Active = true;

                    await PrintMatches(context);
                    await _botApi.SendMessage(context, $"Первая стадия турнира началась. Пошел отсчет времени __**{TimeSpan.FromMinutes(_options.StageTimeoutMinutes).PrettyPrint()}**__ до конца стадии.", GuildThread.EventsTape | GuildThread.TournamentChat);

                    return;
                }

                if (result.IsNotEnoughPlayers)
                {
                    var players = _tournamentApi.RegisteredPlayers;
                    await Log(context, "Not enough players");
                    await _tournamentApi.DropTournament();
                    await _botApi.SendMessage(context, "В данный момент турнир невозможно начать, так как участников недостаточно.\nМинимальное количество участников 4.\nСписок зарегистрированных был обнулен.", GuildThread.EventsTape | GuildThread.TournamentChat, players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());
                    return;
                }

                await Log(context, "Broken state");
            }
            catch (Exception ex)
            {
                await Log(context, nameof(DoStartCurrentTournament) + ':' + ex.ToString());
            }
        }

        private async Task PrintMatches(Context context, bool remindRules = true)
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

                var p1Mention = p1.Item1.IsBot ? p1.Item1.Name : await _botApi.GetMention(context, p1.Item1.DiscordId);
                var p2Mention = p2.Item1.IsBot ? p2.Item1.Name : await _botApi.GetMention(context, p2.Item1.DiscordId);

                builder.AppendLine($"{m.Id + 1}. {p1Mention} {p2Mention} | {m.Map}");
                builder.AppendLine($"**{p1.Item1.Name}** ({p1.Item2})  VS  **{p2.Item1.Name}** ({p2.Item2})");
                builder.AppendLine();
            }

            if (remindRules)
            {
                builder.AppendLine("**Не забывайте использовать DowStats и прикрепленный Steam аккаунт, иначе матч не будет засчитан!**");
                builder.AppendLine();
            }

            await _botApi.SendMessage(context, builder.ToString(), GuildThread.EventsTape | GuildThread.TournamentChat);

            var mentions = Mentions;
            var hostingMentions = mentions.Where(x =>
            {
                return matches.Any(m => m.Player1.Value.Item1.DiscordId == x);
            }).ToArray();

            await _botApi.SendMessage(context, "---\nИгру хостят вот эти ребята:", GuildThread.EventsTape | GuildThread.TournamentChat);
            await _botApi.Mention(context, GuildThread.EventsTape | GuildThread.TournamentChat, hostingMentions);

            var relaxingMentions = mentions.Where(x =>
            {
                return !matches.Any(m => m.Player1.Value.Item1.DiscordId == x || m.Player2.Value.Item1.DiscordId == x);
            }).ToArray();

            if (relaxingMentions.Length > 0)
            {
                await _botApi.SendMessage(context, "---\nНа скамейке отдыхающих на этот раз:", GuildThread.EventsTape | GuildThread.TournamentChat);
                await _botApi.Mention(context, GuildThread.EventsTape | GuildThread.TournamentChat, relaxingMentions);
            }
        }

        public async Task DoStartNextStage(string contextName)
        {
            var context = GetContextByName(contextName);

            try
            {
                await Log(context, "An attempt to start the next stage..");

                var result = await _tournamentApi.TryStartNextStage();

                while (result.IsDone && (await _tournamentApi.TryCompleteCurrentStage()) == CompleteStageResult.Completed)
                    result = await _tournamentApi.TryStartNextStage();

                if (result.IsNoTournament)
                {
                    await Log(context, "No a planned tournament");
                    return;
                }

                if (result.IsPreviousStageIsNotCompleted)
                {
                    await Log(context, "The previous stage is not completed");
                    return;
                }

                if (result.IsTheStageIsTerminal)
                {
                    _scanner.Active = false;

                    await Log(context, "The stage is terminal");

                    var tournamentHeader = _tournamentApi.Header;

                    if (!_tournamentApi.PlayedMatches.Any(x => x.Result.IsWinner))
                    {
                        await _botApi.SendMessage(context, "Определился победитель, но турнир не будет засчитан, так как он полностью состоит из технических поражений. Должна быть сыграна хотя бы одна игра.", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
                        
                        await _botApi.SendMessage(context, $"__**{tournamentHeader} завершен без учета результатов**__\n==================================================================================================\n", GuildThread.EventsTape | GuildThread.TournamentChat);
                        await _tournamentApi.DropTournament();
                        _dataService.IncrementTournamentId();
                        await Log(context, "The tournament is finished without results");
                        return;
                    }

                    await _botApi.SendMessage(context, "Определился победитель турнира!", GuildThread.EventsTape | GuildThread.TournamentChat);

                    var tournamentBundle = await _tournamentApi.BuildAllData();
                    var date = _tournamentApi.StartDate.Value;
                    await _botApi.SendFile(context, tournamentBundle.Image, $"tournament_{tournamentBundle.Tournament.Id}_completed.png", "Полная сетка:", GuildThread.EventsTape | GuildThread.TournamentChat);
                    await UploadTournamentToHistory(context, tournamentBundle);
                    await UpdateLeaderboardAndUploadChangesToHistory(context, tournamentBundle);

                    await _botApi.SendMessage(context, $"__**{tournamentHeader} успешно завершен**__\n==================================================================================================\n", GuildThread.EventsTape | GuildThread.TournamentChat);
                    await _tournamentApi.DropTournament();
                    await Log(context, "The tournament is finished normally");
                    return;
                }

                if (result.IsDone)
                {
                    await Log(context, "The stage has been started..");
                    await _botApi.SendMessage(context, ">>> Начинается следующая стадия турнира! Генерация сетки..", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
                    await _botApi.SendFile(context, await _tournamentApi.RenderTournamentImage(), $"tournament_{_tournamentApi.Id}.png", "Сетка турнира", GuildThread.EventsTape | GuildThread.TournamentChat);

                    _timeline.AddOneTimeEventAfterTime(Event.NewCompleteStage(contextName), TimeSpan.FromMinutes(_options.StageTimeoutMinutes));
                    _scanner.GameTypeFilter = GameType.Type1v1;
                    _scanner.Active = true;

                    await PrintMatches(context, false);
                    await _botApi.SendMessage(context, $"Новая стадия турнира началась. Пошел отсчет времени __**{TimeSpan.FromMinutes(_options.StageTimeoutMinutes).PrettyPrint()}**__ до конца стадии.", GuildThread.EventsTape | GuildThread.TournamentChat);
                    return;
                }

                await Log(context, "Broken state");
            }
            catch (Exception ex)
            {
                await Log(context, nameof(DoStartNextStage) + ':' + ex.ToString());
            }
        }

        public async Task DoCompleteVoting(string contextName)
        {
            var context = GetContextByName(contextName);

            try
            {
                _timeline.RemoveAllEventsWithType(Event.NewCompleteVoting(contextName));
                ActiveVotingButtons = null;
                await Log(context, "An attempt to complete the voting..");

                var result = await _tournamentApi.TryCompleteVoting();

                if (result.IsCompleted)
                {
                    await Log(context, "The voting is completed");
                    return;
                }

                if (result.IsCompletedWithNoEnoughVotes)
                {
                    await Log(context, "The voting has no enough votes");
                    return;
                }

                if (result.IsNoVoting)
                {
                    await Log(context, "There is no voting now");
                    return;
                }

                if (result.IsTheVoteIsOver)
                {
                    await Log(context, "The voting is over");
                    return;
                }

                await Log(context, "Broken state");
            }
            catch (Exception ex)
            {
                await Log(context, nameof(DoCompleteVoting) + ':' + ex.ToString());
            }
        }

        private async Task UpdateLeaderboardAndUploadChangesToHistory(Context context, TournamentBundle bundle)
        {
            await Log(context, "Updating leaderboards");
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

            var (printedChanges, mentions) = PrintChangesAndUpdateUsersInDataService(context, modifiedUsers);
            await ServiceHelpers.RefreshLeaders(context, _botApi, _dataService);

            if (printedChanges != null && mentions != null)
                await _botApi.SendMessage(context, printedChanges, GuildThread.EventsTape | GuildThread.TournamentChat | GuildThread.History, mentions);
        }

        private (string, ulong[]) PrintChangesAndUpdateUsersInDataService(Context context, Dictionary<ulong, (UserData Data, string Name, int AddedScore, int Penalties)> modifiedUsers)
        {
            if (modifiedUsers.Values.Count == 0)
                return (null, null);

            foreach (var info in modifiedUsers.Values.OrderByDescending(x => x.AddedScore))
            {
                var data = info.Data;
                data.Score = data.Score + info.AddedScore;
                data.Penalties = info.Penalties;

                if (!_dataService.UpdateUser(data))
                    Log(context, $"WARNING! Unable to update the users rating. User DiscordId = {data.DiscordId}. User Steamid = {data.SteamId}. Rating = {data.Score}. Penalties = {data.Penalties}");
            }

            var mentionsList = new List<ulong>();
            var builder = new StringBuilder();

            builder.AppendLine();
            builder.AppendLine("--- __**Изменения в рейтинге**__ ---");

            int i = 1;
            foreach (var info in modifiedUsers.Values.OrderByDescending(x => x.AddedScore))
            {
                if (info.AddedScore != 0)
                {
                    mentionsList.Add(info.Data.DiscordId);
                    builder.AppendLine($"{i++}. {info.AddedScore} | {info.Name}");
                }
            }

            builder.AppendLine();

            return (builder.ToString(), mentionsList.ToArray());
        }

        private async Task UploadTournamentToHistory(Context context, TournamentBundle bundle)
        {
            await Log(context, "Uploading the tournament to history");
            var data = new TournamentData()
            { 
                TournamentId = bundle.Tournament.Id,
                SeasonId = bundle.Tournament.SeasonId,
                Date = bundle.Tournament.StartDate.ValueOrDefault(),
                Type = bundle.Tournament.Type,
                Mod = bundle.Tournament.Mod,
                PlayersBans = bundle.Tournament.RegisteredPlayers.Select(x => (int)x.MapBans).ToArray(),
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

            _dataService.StoreTournamentAndIncrementTournamentId(data);

            var tournamentHeader = $"{bundle.Tournament.Type} AutoCup {bundle.Tournament.Id} | {bundle.Tournament.StartDate.Value.PrettyShortDatePrint()}";
            await _botApi.SendMessage(context, $"__**{tournamentHeader}**__ благополучно завершен!\nПоздравляем с победой игрока под ником __**{bundle.Winner.Value.Name}**__. Всем остальным желаем удачи на следующих турнирах!", GuildThread.History, AllPlayersMentions);
            await _botApi.SendFile(context, bundle.Image, $"tournament_{bundle.Tournament.Id}_completed.png", "Сетка:", GuildThread.History);

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
            await _botApi.SendMessage(context, builder.ToString(), GuildThread.History);
        }

        private async Task PrintTimeAndNextEvent(Context context)
        {
            var nextEvent = _timeline.GetNextEventInfo();

            if (nextEvent != null)
            {
                var e = nextEvent;
                await _botApi.SendMessage(context, $"Московское время {GetMoscowTime().PrettyShortTimePrint()}\nСледующее событие '**{e.Event.PrettyPrint()}**' наступит через **{GetTimeBeforeEvent(e).PrettyPrint()}**.", GuildThread.TournamentChat | GuildThread.EventsTape);
            }
        }

        private Context GetContextByName(string contextName)
        {
            return _contextService.GetContext(contextName);
        }

        private Task Log(Context context, string message)
        {
            _logger.LogInformation(message);
            return _botApi.Log(context, message);
        }
    }
}
