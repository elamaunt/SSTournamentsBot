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
        readonly DiscordBotOptions _options;
        readonly IEventsTimeline _timeLine;

        public DiscordBot(DiscordSocketClient client, IEventsTimeline timeLine, IOptions<DiscordBotOptions> options)
        {
            _client = client;
            _timeLine = timeLine;
            _options = options.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Request the instance from the client.
            // Because we're requesting it here first, its targetted constructor will be called and we will receive an active instance.

            _timeLine.RemoveAllEvents();

            _client.Log += async (msg) =>
            {
                await Task.CompletedTask;
                Console.WriteLine(msg);
            };

            _client.Ready += OnReady;

            await _client.LoginAsync(TokenType.Bot, _options.Token);
            await _client.StartAsync();
        }

        private Task OnReady()
        {
            _timeLine.AddPeriodicalEventWithPeriod(Event.StartCheckIn, TimeSpan.FromSeconds(20));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.Ready -= OnReady;
            return _client.StopAsync();
        }
    }
}
