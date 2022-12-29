using Microsoft.Extensions.Logging;
using SSTournamentsBot.Api.Domain;
using System;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class TournamentEventsHandler : IEventsHandler
    {
        readonly ILogger<TournamentEventsHandler> _logger;
        readonly IBotApi _botApi;
        readonly IGameScanner _scanner;
        readonly IEventsTimeline _timeline;
        readonly TournamentApi _tournamentApi;

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
            _logger.LogInformation("An attempt to complete the stage..");

            var result = await _tournamentApi.TryCompleteCurrentStage();

            if (result.IsNoTournament)
            {
                _logger.LogInformation("No a planned tournament");
                return;
            }

            if (result.IsNotAllMatchesFinished)
            {
                _logger.LogInformation("Not all matches finished");
                await _botApi.SendMessage("Не все матчи текущей стадии были завершены за указанное время. Для установления результатов по каждому матчу будет запущено голосование, либо решение будет принято модераторами.", GuildThread.EventsTape | GuildThread.TournamentChat);

                // TODO: start voting. 
                _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromSeconds(30));
                return;
            }

            if (result.IsCompleted)
            {
                _scanner.Active = false;
                _logger.LogInformation("The stage has been completed");
                await _botApi.SendMessage("Стадия была успешно завершена! Следующая стадия начнется после 5-минутного перерыва.", GuildThread.EventsTape | GuildThread.TournamentChat);

                _timeline.AddOneTimeEventAfterTime(Event.StartNextStage, TimeSpan.FromSeconds(10));
                return;
            }

            _logger.LogError("Broken state");
        }

        public async void DoStartCheckIn()
        {
            _logger.LogInformation("An attempt to start checkin stage..");

            var result = await _tournamentApi.TryStartTheCheckIn();

            if (result.IsNoTournament)
            {
                _logger.LogInformation("No a planned tournament");
                return;
            }

            if (result.IsAlreadyStarted)
            {
                _logger.LogInformation("Tournament is running");
                return;
            }

            if (result.IsNotEnoughPlayers)
            {
                var players = _tournamentApi.RegisteredPlayers;

                _logger.LogInformation("Not enough players.");
                await _tournamentApi.DropTournament();
                await _botApi.SendMessage("В данный момент турнир невозможно начать, так как участников недостаточно. Список зарегистрированных был обнулен.", GuildThread.EventsTape | GuildThread.TournamentChat, players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());
                return;
            }

            if (result.IsDone)
            {
                _logger.LogInformation("Checkin stage starting..");

                var players = _tournamentApi.RegisteredPlayers;

                await _botApi.SendMessage("Внимание! Началась стадия чекина на турнир. Всем участникам нужно выполнить команду __**/checkin**__ на турнирном канале для подтверждения своего участия.", GuildThread.EventsTape, players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());
                
                _timeline.AddOneTimeEventAfterTime(Event.StartCurrentTournament, TimeSpan.FromSeconds(30));
                return;
            }

            _logger.LogError("Broken state");
        }

        public async void DoStartCurrentTournament()
        {
            _logger.LogInformation("An attempt to start the tournament..");

            var result = await _tournamentApi.TryStartTheTournament();

            if (result.IsNoTournament)
            {
                _logger.LogInformation("No a planned tournament");
                return;
            }

            if (result.IsAlreadyStarted)
            {
                _logger.LogInformation("Tournament is running");
                return;
            }

            if (result.IsDone)
            {
                _logger.LogInformation("Starting the tournament..");

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

            _logger.LogError("Broken state");
        }

        public async void DoStartNextStage()
        {
            _logger.LogInformation("An attempt to start the next stage..");

            var result = await _tournamentApi.TryStartNextStage();

            while (result.IsDone && (await _tournamentApi.TryCompleteCurrentStage()) == CompleteStageResult.Completed)
                result = await _tournamentApi.TryStartNextStage();

            if (result.IsNoTournament)
            {
                _logger.LogInformation("No a planned tournament");
                return;
            }

            if (result.IsTheStageIsTerminal)
            {
                _logger.LogInformation("The stage is terminal.");
                await _botApi.SendMessage("Определился победитель!", GuildThread.EventsTape | GuildThread.TournamentChat);

                var tournamentImage = await _tournamentApi.RenderTournamentImage();

                await _botApi.SendFile(tournamentImage, "tournament.png", "Сетка турнира", GuildThread.EventsTape | GuildThread.TournamentChat);

                await UploadTournamentToHistory(tournamentImage);
                await UpdateLeaderboard();
                return;
            }

            if (result.IsDone)
            {
                _scanner.Active = true;

                _logger.LogInformation("The stage has been started..");
                await _botApi.SendMessage("Начинается следующая стадия турнира! Генерация сетки..", GuildThread.EventsTape);
                await  _botApi.SendFile(await _tournamentApi.RenderTournamentImage(), "tournament.png", "Сетка турнира", GuildThread.EventsTape | GuildThread.TournamentChat);
                return;
            }

            _logger.LogError("Broken state");
        }

        public async void DoStartPreCheckingTimeVote()
        {
            _logger.LogInformation("An attempt to start the pre checking time vote..");

            var (result, progress) = await _tournamentApi.TryStartVoting(Voting.NewAddTime(VoteAddTimeType.CheckinStart, TimeSpan.FromMinutes(30)), GuildRole.Administrator);

            if (result.IsCompleted)
            {
                _logger.LogInformation("The PreCheckingTime voting has been started..");

                if (progress == null)
                {
                    _logger.LogWarning("No voting progress to publish the message.");
                    return;
                }

                var players = _tournamentApi.RegisteredPlayers;
                var buttonInfos = progress.VoteOptions.Select(x => (x.Item1, x.Item2, BotButtonStyle.Secondary)).ToArray();
                await _botApi.SendButtons("До начала чек-ина в турнире осталось 10 минут.\nСтоит ли отложить чекин турнира на 30 минут?\nГолосовать могут только участники, зарегистрированные на следующий турнир и администрация сервера.", buttonInfos, GuildThread.EventsTape | GuildThread.TournamentChat, players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());
                _timeline.AddOneTimeEventAfterTime(Event.CompleteVoting, TimeSpan.FromMinutes(10));

                return;
            }

            _logger.LogError("Broken state");
        }

        public async void DoCompleteVoting()
        {
            _logger.LogInformation("An attempt to complete the voting..");

            var (result, progress) = await _tournamentApi.TryCompleteVoting();

            if (result.IsCompleted)
            {
                _logger.LogInformation("The voting is completed");
                return;
            }

            if (result.IsNoEnoughVotes)
            {
                _logger.LogInformation("The voting has no enough votes");
                return;
            }

            if (result.IsNoVoting)
            {
                _logger.LogInformation("There is no voting now");
                return;
            }

            if (result.IsTheVoteIsOver)
            {
                _logger.LogInformation("The voting is over");
                return;
            }

            _logger.LogError("Broken state");
        }

        private Task UpdateLeaderboard()
        {
            return Task.CompletedTask;
        }

        private Task UploadTournamentToHistory(byte[] tournamentImage)
        {
            return Task.CompletedTask;
        }
    }
}
