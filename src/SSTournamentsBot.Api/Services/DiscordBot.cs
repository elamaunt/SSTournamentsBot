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
        readonly ITournamentEventsHandler _eventsHandler;

        private volatile bool _isReady;

        public DiscordBot(DiscordSocketClient client,
            TournamentApi tournamentApi,
            IBotApi botApi,
            IEventsTimeline timeLine,
            ITournamentEventsHandler eventsHandler, 
            IOptions<DiscordBotOptions> options)
        {
            _client = client;
            _tournamentApi = tournamentApi;
            _botApi = botApi;
            _timeLine = timeLine;
            _eventsHandler = eventsHandler;
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
                {
                    if (msg.Severity == LogSeverity.Error || msg.Severity == LogSeverity.Critical)
                        await _botApi.SendMessage(s, GuildThread.Logging, 272710324484833281);
                    else
                        await _botApi.SendMessage(s, GuildThread.Logging);
                }
            };

            _client.Ready += OnReady;
            _client.ButtonExecuted += OnButtonExecuted;

            await _client.LoginAsync(TokenType.Bot, _options.Token);
            await _client.StartAsync();
        }

        private Task OnReady()
        {
            _isReady = true;
            _timeLine.AddOneTimeEventAfterTime(Event.StartPreCheckingTimeVote, TimeSpan.FromSeconds(30));
            return Task.CompletedTask;
        }

        private async Task OnButtonExecuted(SocketMessageComponent arg)
        {
            var guildUser = (SocketGuildUser)arg.User;

            var guildRole = GuildRole.Everyone;
            if (guildUser.GuildPermissions.KickMembers)
                guildRole = GuildRole.Moderator;
            if (guildUser.GuildPermissions.Administrator)
                guildRole = GuildRole.Administrator;

            var result = await _tournamentApi.TryAcceptVote(arg.User.Id, int.Parse(arg.Data.CustomId), guildRole);

            if (result == AcceptVoteResult.Accepted)
            {
                await arg.RespondAsync("Ваш голос был учтен");
                return;
            }

            if (result == AcceptVoteResult.NoVoting)
            {
                await arg.RespondAsync("Голосование сейчас не проводится");
                return;
            }

            if (result == AcceptVoteResult.TheVoteIsOver)
            {
                await arg.RespondAsync("Голосования уже завершилось");
                return;
            }

            if (result == AcceptVoteResult.YouCanNotVote)
            {
                await arg.RespondAsync("Вы не имеете права участвовать в этом голосовании");
                return;
            }

            if (result == AcceptVoteResult.AlreadyVoted)
            {
                await arg.RespondAsync("Вы уже проголосовали");
                return;
            }

            if (result == AcceptVoteResult.CompletedByThisVote)
            {
                _eventsHandler.DoCompleteVoting();
                await arg.RespondAsync($"{arg.User.Mention} завершает голосование голосом администрации.");
                return; 
            }

            await arg.RespondAsync("Ошибка во время обработка запроса");
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
