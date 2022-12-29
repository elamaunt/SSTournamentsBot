using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SSTournamentsBot.Api.Domain;
using System;
using System.Linq;
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
            _timeLine.AddPeriodicalEventWithPeriod(Event.StartPreCheckingTimeVote, TimeSpan.FromSeconds(20));
            return Task.CompletedTask;
        }

        private async Task OnButtonExecuted(SocketMessageComponent arg)
        {
            var main = _client.GetGuild(_options.MainGuildId);
            var role = main.GetRole(arg.User.Id);

            var guildRole = GuildRole.Everyone;

            if (role.Permissions.KickMembers)
                guildRole = GuildRole.Moderator;

            var (result, progress) = await _tournamentApi.TryAcceptVote(arg.User.Id, arg.Data.CustomId, guildRole);

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
                await arg.RespondAsync($"{arg.User.Mention} завершает голосование своим голосом.");
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
                            await _botApi.SendMessage("Результат голосования: ОТКЛОНЕН.", GuildThread.EventsTape | GuildThread.TournamentChat, players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());
                            break;
                        case "1":
                            await _botApi.SendMessage("Результат голосования: ПРИНЯТ.", GuildThread.EventsTape | GuildThread.TournamentChat, players.Where(x => !x.IsBot).Select(x => x.DiscordId).ToArray());
                            await ApplyVotingResult(progress.Voting);
                            break;
                        default:
                            await _botApi.SendMessage("Ошибка обработки результата.", GuildThread.EventsTape);
                            break;
                    }
                }
                return; 
            }

            await arg.RespondAsync("Ошибка во время обработка запроса");
        }

        private Task ApplyVotingResult(Voting voting)
        {
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
