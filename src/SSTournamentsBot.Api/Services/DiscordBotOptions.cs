namespace SSTournamentsBot.Api.Services
{
    public class DiscordBotOptions
    {
        public string Token { get; set; }
        public ulong MainGuildId { get; set; }
        public ulong TournamentThreadId { get; set; }
        public ulong EventsThreadId { get; set; }
        public ulong HistoryThreadId { get; set; }
        public ulong LeaderboardThreadId { get; set; }
        public ulong LoggingThreadId { get; set; }
    }
}
