using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class ContextService : IHostedService, IContextService
    {
        readonly ConcurrentDictionary<string, Context> _contexts = new ConcurrentDictionary<string, Context>();
        readonly ConcurrentDictionary<ulong, (string, Context)> _contextsByChannels = new ConcurrentDictionary<ulong, (string, Context)>();
        readonly IServiceProvider _serviceProvider;
        readonly DiscordBotOptions _options;

        public ContextService(IServiceProvider serviceProvider, IOptions<DiscordBotOptions> options)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var pair in _options.Setups)
            {
                var name = pair.Key;
                var opt = pair.Value;

                var context = new Context(name, GetService<TournamentApi>(), GetService<ITournamentEventsHandler>(), opt);

                if (!_contexts.TryAdd(name, context))
                    throw new InvalidOperationException("Context must have an unique name");

                foreach (var channelPair in opt.Channels)
                {
                    var locale = channelPair.Key;
                    var scope = channelPair.Value;

                    _contextsByChannels.TryAdd(scope.EventsThreadId, (locale, context));
                    _contextsByChannels.TryAdd(scope.HistoryThreadId, (locale, context));
                    _contextsByChannels.TryAdd(scope.LeaderboardThreadId, (locale, context));
                    _contextsByChannels.TryAdd(scope.LoggingThreadId, (locale, context));
                    _contextsByChannels.TryAdd(scope.VotingsTapeThreadId, (locale, context));
                    _contextsByChannels.TryAdd(scope.TournamentThreadId, (locale, context));
                }
            }

            return Task.CompletedTask;
        }

        public Context GetMainContext()
        {
            throw new NotImplementedException();
        }

        private T GetService<T>() where T : class
        {
            return (T)_serviceProvider.GetService(typeof(T));
        }

        public (string, Context) GetLocaleAndContext(ulong channel)
        {
            return _contextsByChannels[channel];
        }

        public Context GetContext(string name)
        {
            return _contexts[name];
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
