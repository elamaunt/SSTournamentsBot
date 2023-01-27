using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class Context
    {
        public string Name { get; }
        public TournamentApi TournamentApi { get; }
        public ITournamentEventsHandler EventsHandler { get; }
        public IBotApi BotApi { get; }
        public SetupOptions Options { get; }

        public Context(string name, TournamentApi api, ITournamentEventsHandler eventsHandler, IBotApi botApi, SetupOptions options)
        {
            Name = name;
            TournamentApi = api;
            EventsHandler = eventsHandler;
            BotApi = botApi;
            Options = options;
        }
    }
}