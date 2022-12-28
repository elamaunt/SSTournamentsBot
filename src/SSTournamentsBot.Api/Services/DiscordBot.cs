using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class DiscordBot : IHostedService
    {
        readonly DiscordSocketClient _client;
        readonly TournamentApi _tournamentApi;
        readonly IBotApi _botApi;
        readonly DiscordBotOptions _options;
        readonly IEventsTimeline _timeLine;
        private volatile bool _isReady;

        public DiscordBot(DiscordSocketClient client, TournamentApi tournamentApi, IBotApi botApi, IEventsTimeline timeLine, IOptions<DiscordBotOptions> options)
        {
            _client = client;
            _tournamentApi = tournamentApi;
            _botApi = botApi;
            _timeLine = timeLine;
            _options = options.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _timeLine.RemoveAllEvents();

            _client.Log += async (msg) =>
            {
                var s = msg.ToString();
                Console.WriteLine(s);

                if (_isReady)
                    await _botApi.SendMessage(s, GuildThread.Logging);
            };

            _client.Ready += OnReady;
            _client.ButtonExecuted += OnButtonExecuted;

            await _client.LoginAsync(TokenType.Bot, _options.Token);
            await _client.StartAsync();
        }

        private async Task OnButtonExecuted(SocketMessageComponent arg)
        {
            var result = await _tournamentApi.TryAcceptVote(arg.Data.CustomId);

            if (result == AcceptVoteResult.Accepted)
            {
                await arg.RespondAsync("Ваш голос был учтен");
                return;
            }

            if (result == AcceptVoteResult.NoVoting)
            {
                await arg.RespondAsync("Голосования сейчас не проводится");
                return;
            }

            if (result == AcceptVoteResult.TheVoteIsOver)
            {
                await arg.RespondAsync("Голосования уже завершилось");
                return;
            }

            if (result == AcceptVoteResult.YouCanNotVote)
            {
                await arg.RespondAsync("Вы не имеет права участвовать в этом голосовании");
                return;
            }

            if (result == AcceptVoteResult.AlreadyVoted)
            {
                await arg.RespondAsync("Вы уже проголосовали");
                return;
            }

            await arg.RespondAsync("Ошибка во время обработка запроса");
        }

        private Task OnReady()
        {
            _isReady = true;
            _timeLine.AddPeriodicalEventWithPeriod(Event.StartCheckIn, TimeSpan.FromSeconds(20));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.Ready -= OnReady;
            _client.ButtonExecuted -= OnButtonExecuted;
            _isReady = false;
            return _client.StopAsync();
        }
    }
}
