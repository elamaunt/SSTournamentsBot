using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class Context
    {
        public string Name { get; }
        public Auto1v1Api AutoApi { get; }
        public Tournament1v1Api TournamentApi { get; }
        public ITournamentEventsHandler EventsHandler { get; }
        public IBotApi BotApi { get; }
        public SetupOptions Options { get; }

        public Context(string name, Tournament1v1Api tournamentApi, Auto1v1Api autoApi, ITournamentEventsHandler eventsHandler, IBotApi botApi, SetupOptions options)
        {
            Name = name;
            TournamentApi = tournamentApi;
            AutoApi = autoApi;
            EventsHandler = eventsHandler;
            BotApi = botApi;
            Options = options;
        }
    }
}