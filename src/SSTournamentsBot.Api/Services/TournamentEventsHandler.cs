using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
using SSTournamentsBot.Api.DataDomain;
using SSTournamentsBot.Api.Domain;
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
            TournamentApi tournamentApi)
        {
            _logger = logger;
            _dataService = dataService;
            _botApi = botApi;
            _scanner = scanner;
            _timeline = timeline;
            _tournamentApi = tournamentApi;
        }

        public async void DoCompleteStage()
        {
            await Log("An attempt to complete the stage..");
            _timeline.RemoveAllEventsWithType(Event.CompleteStage);

            var result = await _tournamentApi.TryCompleteCurrentStage();

            if (result.IsNoTournament)
            {
                await Log("No a planned tournament");
                return;
            }

            if (result.IsNotAllMatchesFinished)
            {
                var activeMatch = _tournamentApi.ActiveMatches.FirstOrDefault(x => x.Result.IsNotCompleted);

                if (activeMatch != null)
                {
                    await Log("Not all matches finished");

                    var p1 = activeMatch.Player1.Value.Item1;
                    var p2 = activeMatch.Player2.Value.Item1;

                    var voting = CreateVoting(">>> Минутное голосование по незавершенному матчу. Кому присудить техническое поражение?", 
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
                                    await _botApi.SendMessage(">>> Результат голосования не принят, так как голоса разделились поровну, либо никто не проголосовал.\nНо решение все равно нужно принять, поэтому техническое поражение выдается случайному игроку.", GuildThread.EventsTape | GuildThread.TournamentChat);
                                    await Task.Delay(2000);
                                    x = new FSharpOption<int>(new Random().NextDouble() >= 0.5 ? 0 : 1);
                                }

                                switch (x.Value)
                                {
                                    case 0:
                                        await _tournamentApi.TryLeaveUser(p1.DiscordId, p1.SteamId);
                                        await _botApi.SendMessage($"> Присуждено техническое поражение игроку под ником **{p1.Name}**.", GuildThread.EventsTape | GuildThread.TournamentChat, p1.DiscordId);
                                        break;
                                    case 1:
                                        await _tournamentApi.TryLeaveUser(p2.DiscordId, p2.SteamId);
                                        await _botApi.SendMessage($"> Присуждено техническое поражение игроку под ником **{p2.Name}**.", GuildThread.EventsTape | GuildThread.TournamentChat, p2.DiscordId);
                                        break;
                                    default:
                                        break;
                                }

                                if (_tournamentApi.IsAllActiveMatchesCompleted())
                                {
                                    _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromSeconds(5));
                                }
                            });

                            return SharedUnit;
                    }));

                    voting = AddVoteOption(voting, p1.Name, BotButtonStyle.Danger);
                    voting = AddVoteOption(voting, p2.Name, BotButtonStyle.Danger);

                    var startVotingResult = await _tournamentApi.TryStartVoting(voting);

                    if (startVotingResult.IsCompleted)
                    {
                        ActiveVotingButtons = await _botApi.SendVotingButtons(voting.Message, voting.Options, GuildThread.VotingsTape | GuildThread.TournamentChat);
                        _timeline.AddOneTimeEventAfterTime(Event.CompleteVoting, TimeSpan.FromMinutes(1));
                    }

                    _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromSeconds(70));
                    return;
                }
                else
                {
                    _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromMinutes(1));
                    return;
                }
            }

            if (result.IsCompleted)
            {
                _scanner.Active = false;
                await Log("The stage has been completed");

                if (_tournamentApi.ActiveMatches.Length <= 1)
                {
                    DoStartNextStage();
                }
                else
                {
                    await _botApi.SendMessage("Стадия была успешно завершена! Следующая стадия начнется после 5-минутного перерыва.", GuildThread.EventsTape | GuildThread.TournamentChat);

                    _timeline.AddOneTimeEventAfterTime(Event.StartNextStage, TimeSpan.FromSeconds(30));
                }
                return;
            }

            await Log("Broken state");
        }

        public async void DoStartCheckIn()
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
                await _botApi.SendMessage("Турнир отменяется, так как участников недостаточно. Список зарегистрированных был обнулен.", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
                await PrintTimeAndNextEvent();
                return;
            }

            if (result.IsDone)
            {
                await Log("Checkin stage starting..");

                await _botApi.SendMessage("Внимание! Началась стадия чекина на турнир. Всем участникам нужно выполнить команду __**/checkin**__ на турнирном канале для подтверждения своего участия.", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
                
                _timeline.AddOneTimeEventAfterTime(Event.StartCurrentTournament, TimeSpan.FromSeconds(30));
                return;
            }

            await Log("Broken state");
        }

        public async void DoStartCurrentTournament()
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

                var players = _tournamentApi.RegisteredPlayers;

                await _botApi.SendMessage("Начинается турнир. В процессе генерации сетки..", GuildThread.EventsTape | GuildThread.TournamentChat, players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());
                await _botApi.SendFile(await _tournamentApi.RenderTournamentImage(), "tournament.png", "Сетка турнира", GuildThread.EventsTape | GuildThread.TournamentChat);
                _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromSeconds(30));
                _scanner.GameTypeFilter = GameType.Type1v1;
                _scanner.Active = true;

                await PrintMatches();

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

        private async Task PrintMatches(bool remindRules = true)
        {
            var builder = new StringBuilder();
            var matches = _tournamentApi.ActiveMatches;

            builder.AppendLine(">>> **Матчи, которые должны быть сыграны в текущей стадии турнира:**");
            for (int i = 0; i < matches.Length; i++)
            {
                var m = matches[i];

                var p1 = m.Player1.Value;
                var p2 = m.Player1.Value;

                var p1Mention = p1.Item1.IsBot ? p1.Item1.Name : await _botApi.GetMention(p1.Item1.DiscordId);
                var p2Mention = p2.Item1.IsBot ? p2.Item1.Name : await _botApi.GetMention(p2.Item1.DiscordId);

                builder.AppendLine($"{p1Mention} {p2Mention}");
                builder.AppendLine($"{m.Id + 1}. **{p1.Item1.Name}** ({p1.Item2})  VS  **{p2.Item1.Name}** ({p2.Item2})       -       {m.Map}");
            }

            if (remindRules)
            {
                builder.AppendLine();
                builder.AppendLine("**Не забывайте использовать DowStats и прикрепленный Steam аккаунт, иначе матч не будет засчитан!**");
            }

            await _botApi.SendMessage(builder.ToString(), GuildThread.EventsTape | GuildThread.TournamentChat);

            var hostingMentions = Mentions.Where(x =>
            {
                return matches.Any(m => m.Player1.Value.Item1.DiscordId == x);
            }).ToArray();

            await _botApi.SendMessage("---\nИгру хостят вот эти ребята:", GuildThread.EventsTape | GuildThread.TournamentChat, hostingMentions);

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
            await Log("An attempt to start the next stage..");

            var result = await _tournamentApi.TryStartNextStage();

            while (result.IsDone && (await _tournamentApi.TryCompleteCurrentStage()) == CompleteStageResult.Completed)
                result = await _tournamentApi.TryStartNextStage();

            if (result.IsNoTournament)
            {
                await Log("No a planned tournament");
                return;
            }

            if (result.IsTheStageIsTerminal)
            {
                await Log("The stage is terminal.");
                await _botApi.SendMessage("Определился победитель турнира!", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);

                var tournamentBundle = await _tournamentApi.BuildAllData();

                await _botApi.SendFile(tournamentBundle.Image, "tournament.png", "Полная сетка турнира", GuildThread.EventsTape | GuildThread.TournamentChat);

                await UploadTournamentToHistory(tournamentBundle);
                await UpdateLeaderboard(tournamentBundle);
                await _tournamentApi.DropTournament();
                await Task.Delay(10000);
                await PrintTimeAndNextEvent();
                await Log("The tournament is finished normally");
                return;
            }

            if (result.IsDone)
            {
                await Log("The stage has been started..");
                await _botApi.SendMessage(">>> Начинается следующая стадия турнира! Генерация сетки..", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
                await  _botApi.SendFile(await _tournamentApi.RenderTournamentImage(), "tournament.png", "Сетка турнира", GuildThread.EventsTape | GuildThread.TournamentChat);

                _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromSeconds(60));
                _scanner.GameTypeFilter = GameType.Type1v1;
                _scanner.Active = true;

                await PrintMatches();
                return;
            }

            await Log("Broken state");
        }

        public async void DoStartPreCheckingTimeVote()
        {
            await Log("An attempt to start the pre checking time vote..");

            var voting = CreateVoting(">>> *До начала чек - ина в турнире осталось 10 минут.*\nСтоит ли отложить чекин турнира и его начало на 30 минут ?\n\nГолосовать могут только участники, зарегистрированные на следующий турнир и администрация сервера.",
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
                                        _timeline.AddTimeToNextEventWithType(Event.StartCheckIn, TimeSpan.FromMinutes(1));
                                        await _botApi.SendMessage($"> Начало турнира и чекин отложены на 30 минут.", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
                                        break;
                                    case 1:
                                        await _botApi.SendMessage($"> Начало турнира будет в стандартное время.", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
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
                _timeline.AddOneTimeEventAfterTime(Event.CompleteVoting, TimeSpan.FromSeconds(30));
                _timeline.AddOneTimeEventAfterTime(Event.StartCheckIn, TimeSpan.FromMinutes(1));
                return;
            }

            await Log("Broken state");
        }

        public async void DoCompleteVoting()
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

        private async Task UpdateLeaderboard(TournamentBundle bundle)
        {
            await Log("Updating leaderboards");
            var modifiedUsers = new Dictionary<ulong, (UserData, int)>();

            (UserData Data, int AddedScore) userInfo;

            for (int i = 0; i < bundle.PlayedMatches.Length; i++)
            {
                var match = bundle.PlayedMatches[i];

                if (match.Result.IsWinner)
                {
                    var matchWinner = ((MatchResult.Winner)match.Result).Item1;

                    if (!modifiedUsers.TryGetValue(matchWinner.SteamId, out userInfo))
                        userInfo = modifiedUsers[matchWinner.SteamId] = (_dataService.FindUserByDiscordId(matchWinner.DiscordId), 0);

                    modifiedUsers[matchWinner.SteamId] = (userInfo.Data, userInfo.AddedScore + 10);
                    continue;
                }

                if (match.Result.IsTechnicalWinner)
                {
                    var matchWinner = ((MatchResult.TechnicalWinner)match.Result).Item1;

                    var loser = match.Player1.Value.Item1.SteamId == matchWinner.SteamId ? match.Player2.Value.Item1 : match.Player1.Value.Item1;

                    if (!modifiedUsers.TryGetValue(loser.SteamId, out userInfo))
                        userInfo = modifiedUsers[loser.SteamId] = (_dataService.FindUserByDiscordId(loser.DiscordId), 0);

                    modifiedUsers[loser.SteamId] = (userInfo.Data, userInfo.AddedScore - 10);
                    continue;
                }
            }

            var tournamentWinner = bundle.Winner.ValueOrDefault();

            if (tournamentWinner != null && modifiedUsers.TryGetValue(tournamentWinner.SteamId, out userInfo))
            {
                modifiedUsers[tournamentWinner.SteamId] = (userInfo.Data, userInfo.AddedScore * 2);
            }

            var builder = new StringBuilder();

            builder.AppendLine("--- Изменения в рейтинге ---");
            builder.AppendLine();

            foreach (var info in modifiedUsers.Values.OrderByDescending(x => x.Item2))
            {
                info.Item1.Score += info.Item2;
                _dataService.UpdateUser(info.Item1);
                builder.AppendLine($"{info.Item2}   -   {await _botApi.GetUserName(info.Item1.DiscordId)}");
            }

            var mentions = modifiedUsers.Values.Select(x => x.Item1.DiscordId).ToArray();
            await _botApi.SendMessage(builder.ToString(), GuildThread.EventsTape | GuildThread.TournamentChat, mentions);

            builder.Clear();

            var leaders = _dataService.LoadLeaders();

            builder.AppendLine("--- Таблица лидеров ---");
            builder.AppendLine();

            for (int i = 0; i < leaders.Length; i++)
            {
                var user = leaders[i];

                builder.AppendLine($"{i+1}. {user.Score}   {await _botApi.GetUserName(user.DiscordId)}");
            }

            builder.AppendLine();

            await _botApi.ModifyLastMessage(builder.ToString(), GuildThread.Leaderboard);
            await _botApi.ModifyLastMessage("Таблица лидеров была обновлена.", GuildThread.EventsTape | GuildThread.TournamentChat);
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
            await _botApi.SendMessage($"*Daily Tournament* благополучно завершен!\nПоздравляем с победой *{bundle.Winner.Value.Name}*.", GuildThread.History, Mentions);
            await _botApi.SendFile(bundle.Image, "tournament.png", "Сетка турнира", GuildThread.History);
        }

        private async Task PrintTimeAndNextEvent()
        {
            var nextEvent = _timeline.GetNextEventInfo();

            if (nextEvent != null)
            {
                var e = nextEvent;
                await _botApi.SendMessage($"Московское время {GetMoscowTime()}\nСледующее событие {e.Event} наступит через {GetTimeBeforeEvent(e).PrettyPrint()}.", GuildThread.TournamentChat | GuildThread.EventsTape);
            }
        }

        private Task Log(string message)
        {
            _logger.LogInformation(message);
            return _botApi.Log(message);
        }
    }
}
