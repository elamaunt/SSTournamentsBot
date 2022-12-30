using Microsoft.Extensions.Logging;
using SSTournamentsBot.Api.Domain;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class TournamentEventsHandler : ITournamentEventsHandler, IVoteHandler
    {
        readonly ILogger<TournamentEventsHandler> _logger;
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

        public TournamentEventsHandler(ILogger<TournamentEventsHandler> logger, IBotApi botApi, IGameScanner scanner, IEventsTimeline timeline, TournamentApi tournamentApi)
        {
            _logger = logger;
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
                await Log("Not all matches finished");
                await _botApi.SendMessage("Не все матчи текущей стадии были завершены за указанное время. Для установления результатов по каждому матчу будет запущено голосование, либо решение будет принято модераторами.", GuildThread.EventsTape | GuildThread.TournamentChat);

                // TODO: start voting. Remove the line below: 
                _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromSeconds(30));
                return;
            }

            if (result.IsCompleted)
            {
                _scanner.Active = false;
                await Log("The stage has been completed");
                await _botApi.SendMessage("Стадия была успешно завершена! Следующая стадия начнется после 5-минутного перерыва.", GuildThread.EventsTape | GuildThread.TournamentChat);

                _timeline.AddOneTimeEventAfterTime(Event.StartNextStage, TimeSpan.FromSeconds(10));
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
                await _botApi.SendMessage("Турнир отменяется, так как участников недостаточно. Список зарегистрированных был обнулен. ", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
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

                await _botApi.SendMessage("Начинается турнир. В процессе генерации сетки..", GuildThread.EventsTape, players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());
                await _botApi.SendFile(await _tournamentApi.RenderTournamentImage(), "tournament.png", "Сетка турнира", GuildThread.EventsTape | GuildThread.TournamentChat);
                _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromSeconds(30));
                _scanner.GameTypeFilter = GameType.Type1v1;
                _scanner.Active = true;
                return;
            }

            if (result.IsNotEnoughPlayers)
            {
                var players = _tournamentApi.RegisteredPlayers;
                _logger.LogInformation("Not enough players");
                await _tournamentApi.DropTournament();
                await _botApi.SendMessage("В данный момент турнир невозможно начать, так как участников недостаточно. Список зарегистрированных был обнулен.", GuildThread.EventsTape | GuildThread.TournamentChat, players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());
                return;
            }

            await Log("Broken state");
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

                var tournamentImage = await _tournamentApi.RenderTournamentImage();

                await _botApi.SendFile(tournamentImage, "tournament.png", "Сетка турнира", GuildThread.EventsTape | GuildThread.TournamentChat);

                await UploadTournamentToHistory(tournamentImage);
                await UpdateLeaderboard();
                return;
            }

            if (result.IsDone)
            {
                _scanner.Active = true;

                await Log("The stage has been started..");
                await _botApi.SendMessage(">>> Начинается следующая стадия турнира! Генерация сетки..", GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
                await  _botApi.SendFile(await _tournamentApi.RenderTournamentImage(), "tournament.png", "Сетка турнира", GuildThread.EventsTape | GuildThread.TournamentChat);
                return;
            }

            await Log("Broken state");
        }

        public async void DoStartPreCheckingTimeVote()
        {
            await Log("An attempt to start the pre checking time vote..");

            var (result, progress) = await _tournamentApi.TryStartVoting(Voting.NewAddTime(VoteAddTimeType.CheckinStart, TimeSpan.FromMinutes(1)), GuildRole.Administrator);

            if (result.IsCompleted)
            {
                await Log("The PreCheckingTime voting has been started..");

                if (progress == null)
                {
                    await Log("No voting progress to publish the message.");
                    return;
                }

                var players = _tournamentApi.RegisteredPlayers;
                var buttonInfos = progress.VoteOptions.Select(x => (x.Item1, x.Item2, BotButtonStyle.Secondary)).ToArray();
                ActiveVotingButtons = await _botApi.SendVotingButtons(">>> **До начала чек-ина в турнире осталось 10 минут.**\nСтоит ли отложить чекин турнира на 30 минут?\n\nГолосовать могут только участники, зарегистрированные на следующий турнир и администрация сервера.", buttonInfos, GuildThread.EventsTape | GuildThread.TournamentChat, Mentions);
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

            var (result, progress) = await _tournamentApi.TryCompleteVoting();

            if (result.IsCompleted)
            {
                await Log("The voting is completed");

                var players = _tournamentApi.RegisteredPlayers;

                var (_, option) = progress.CompletedWithResult.Value;

                var value = option.ValueOrDefault();

                if (value == null)
                    await _botApi.SendMessage("Результат голосования не был принят: недостаточно проголосовавших, либо голоса разделились поровну.", GuildThread.EventsTape | GuildThread.TournamentChat, players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());
                else
                {
                    switch (value)
                    {
                        case "0":
                            await _botApi.SendMessage("> Результат голосования: ОТКЛОНЕН.", GuildThread.EventsTape | GuildThread.TournamentChat, players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());
                            break;
                        case "1":
                            await _botApi.SendMessage("> Результат голосования: ПРИНЯТ.", GuildThread.EventsTape | GuildThread.TournamentChat, players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());

                            await Log("Handling voting effect..");
                            SwitchVote(progress.Voting, this);
                            break;
                        default:
                            await _botApi.SendMessage("Ошибка обработки результата.", GuildThread.EventsTape);
                            break;
                    }
                }

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

        private Task UpdateLeaderboard()
        {
            return Task.CompletedTask;
        }

        private Task UploadTournamentToHistory(byte[] tournamentImage)
        {
            return Task.CompletedTask;
        }

        public void HandleVoteKick(ulong discrodId, string name)
        {
            throw new NotImplementedException();
        }

        public void HandleVoteBan(ulong discrodId, string name)
        {
            throw new NotImplementedException();
        }

        public async void HandleVoteAddTime(VoteAddTimeType timeType, TimeSpan time)
        {
            if (timeType.IsCheckinStart)
            {
                _timeline.AddTimeToNextEventWithType(Event.StartCheckIn, time);
                await _botApi.SendMessage($"Стадия чек-ина и старт турнира отложены на время {time}.", GuildThread.TournamentChat | GuildThread.EventsTape, Mentions);

                var moscowTime = GetMoscowTime();
                var nextEvent = _timeline.GetNextEventInfo();

                if (nextEvent.HasValue)
                {
                    var e = nextEvent.Value;
                    await _botApi.SendMessage($"Московское время {moscowTime}\nСледующее событие {e.Item1} наступит через {(e.Date + (e.Period ?? TimeSpan.Zero)) - moscowTime}", GuildThread.TournamentChat | GuildThread.EventsTape);
                }
                return;
            }

            if (timeType.IsStageCompletion)
            {
                _timeline.AddTimeToNextEventWithType(Event.CompleteStage, time);
                await _botApi.SendMessage($"Стадия турнира продлена на время {time}.", GuildThread.TournamentChat | GuildThread.EventsTape, Mentions);

                var moscowTime = GetMoscowTime();
                var nextEvent = _timeline.GetNextEventInfo();

                if (nextEvent.HasValue)
                {
                    var e = nextEvent.Value;
                    await _botApi.SendMessage($"Московское время {moscowTime}\nСледующее событие {e.Item1} наступит через {(e.Date + (e.Period ?? TimeSpan.Zero)) - moscowTime}", GuildThread.TournamentChat | GuildThread.EventsTape);
                }

                return;
            }
        }

        public void HandleVoteRevertMatchResult(int matchId)
        {
            throw new NotImplementedException();
        }

        private Task Log(string message)
        {
            _logger.LogInformation(message);
            return _botApi.Log(message);
        }
    }
}
