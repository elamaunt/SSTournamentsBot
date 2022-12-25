using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class TournamentEventsHandler : IEventsHandler
    {
        readonly ILogger<TournamentEventsHandler> _logger;
        readonly IBotApi _botApi;
        readonly IEventsTimeline _timeline;
        readonly TournamentApi _tournamentApi;

        public TournamentEventsHandler(ILogger<TournamentEventsHandler> logger, IBotApi botApi, IEventsTimeline timeline, TournamentApi tournamentApi)
        {
            _logger = logger;
            _botApi = botApi;
            _timeline = timeline;
            _tournamentApi = tournamentApi;
        }

        public void DoCompleteStage()
        {
            _logger.LogInformation("An attempt to complete the stage..");

            var result = _tournamentApi.TryCompleteCurrentStage();

            if (result.IsNoTournament)
            {
                _logger.LogInformation("No a planned tournament");
                return;
            }

            if (result.IsNotAllMatchesFinished)
            {
                _logger.LogInformation("Not all matches finished");
                _botApi.SendMessage("Не все матчи текущей стадии были завершены. Для установления резултатов по каждому матчу будет запущено голосование.");

                // TODO: start voting
                _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromSeconds(30));
                return;
            }

            if (result.IsCompleted)
            {
                _logger.LogInformation("The stage has been completed");
                _botApi.SendMessage("Стадия была успешно завершена! Следующая стадия начнется после 5-минутного перерыва.");

                _timeline.AddOneTimeEventAfterTime(Event.StartNextStage, TimeSpan.FromSeconds(10));
                return;
            }

            _logger.LogError("Broken state");
        }

        public void DoCompleteVoting()
        {
            _botApi.TryCompleteVoting();
        }

        public void DoStartCheckIn()
        {
            _logger.LogInformation("An attempt to start checkin stage..");

            var result = _tournamentApi.TryStartTheCheckIn();

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
                _logger.LogInformation("Not enough players.");
                _tournamentApi.DropTournament();
                _botApi.SendMessage("В данный момент турнир невозможно начать, так как участников недостаточно. Список зарегистрированных был обнулен.");
                return;
            }

            if (result.IsDone)
            {
                _logger.LogInformation("Checkin stage starting..");

                var players = _tournamentApi.RegisteredPlayers;

                _botApi.SendMessage("Внимание! Началась стадия чекина на турнир. Всем участникам нужно выполнить команду __**/checkin**__ для подтверждения своего участия.", players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());
                
                // TODO: chekins
                _timeline.AddOneTimeEventAfterTime(Event.StartCurrentTournament, TimeSpan.FromSeconds(1));

                return;
            }

            _logger.LogError("Broken state");
        }

        public void DoStartCurrentTournament()
        {
            var result = _tournamentApi.TryStartTheTournament();

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

                _botApi.SendMessage("Начинается турнир. В процессе генерации сетки..", players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());
                _botApi.SendFile(_tournamentApi.RenderTournamentImage(), "tournament.png", "Сетка турнира");
                _timeline.AddOneTimeEventAfterTime(Event.CompleteStage, TimeSpan.FromSeconds(30));
                return;
            }

            if (result.IsNotEnoughPlayers)
            {
                _logger.LogInformation("Not enough players");
                _tournamentApi.DropTournament();
                _botApi.SendMessage("В данный момент турнир невозможно начать, так как участников недостаточно. Список зарегистрированных был обнулен.");
                return;
            }

            _logger.LogError("Broken state");
        }

        public void DoStartNextStage()
        {
            var result = _tournamentApi.TryStartNextStage();

            while (result.IsDone && _tournamentApi.TryCompleteCurrentStage() == CompleteStageResult.Completed)
                result = _tournamentApi.TryStartNextStage();

            if (result.IsNoTournament)
            {
                _logger.LogInformation("No a planned tournament");
                return;
            }

            if (result.IsTheStageIsTerminal)
            {
                _logger.LogInformation("The stage is terminal.");
                _botApi.SendMessage("Определился победитель!");
                _botApi.SendFile(_tournamentApi.RenderTournamentImage(), "tournament.png", "Сетка турнира");
                return;
            }

            if (result.IsDone)
            {
                _logger.LogInformation("The stage has been started..");
                _botApi.SendMessage("Начинается следующая стадия турнира! Генерация сетки..");
                _botApi.SendFile(_tournamentApi.RenderTournamentImage(), "tournament.png", "Сетка турнира");
                return;
            }

            _logger.LogError("Broken state");
        }

    }
}
