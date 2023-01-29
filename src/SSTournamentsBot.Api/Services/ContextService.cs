using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class ContextService : IContextService
    {
        readonly ConcurrentDictionary<string, Context> _contexts = new ConcurrentDictionary<string, Context>();
        readonly ConcurrentDictionary<ulong, (string, Context)> _contextsByChannels = new ConcurrentDictionary<ulong, (string, Context)>();
        readonly IServiceProvider _serviceProvider;
        readonly DiscordBotOptions _options;

        Context _mainContext;

        volatile int _initialized = 0;
        readonly object _lock = new object();

        public ContextService(IServiceProvider serviceProvider, IOptions<DiscordBotOptions> options)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
        }

        private Mod ResolveMod(string mod)
        {
            if (mod == "dxp2")
                return Mod.Soulstorm;

            if (mod == "tournamentpatch")
                return Mod.TPMod;

            throw new ArgumentException("Invalid mod for setup: "+ mod);
        }

        public Context GetMainContext()
        {
            InitializeIfNeeded();
            return _mainContext;
        }

        private void InitializeIfNeeded()
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
            {
                foreach (var pair in _options.Setups)
                {
                    var name = pair.Key;
                    var opt = pair.Value;

                    var tournamentApi = GetService<TournamentApi>();
                    tournamentApi.Mod = ResolveMod(opt.Mod);
                    var context = new Context(name, tournamentApi, GetService<ITournamentEventsHandler>(), GetService<IBotApi>(), opt);

                    if (!_contexts.TryAdd(name, context))
                        throw new InvalidOperationException("Context must have an unique name. Name duplicate: " + name);

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

                _mainContext = _contexts.First(x => x.Key == "Soulstorm").Value ?? _contexts.First().Value;
            }
        }

        private T GetService<T>() where T : class
        {
            return (T)_serviceProvider.GetService(typeof(T));
        }

        public (string, Context) GetLocaleAndContext(ulong channel)
        {
            InitializeIfNeeded();
            return _contextsByChannels[channel];
        }

        public Context GetContext(string name)
        {
            InitializeIfNeeded();
            return _contexts[name];
        }

        public IEnumerable<Context> AllContexts
        {
            get
            {
                InitializeIfNeeded();
                return  _contexts.Values;
            }
        }
    }
}
