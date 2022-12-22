namespace SSTournamentsBot.Api.Services
{
    public class TournamentEventsOptions
    {
        public int CheckInTimeoutSeconds { get; set; }
        public int ReCheckInTimeoutSeconds { get; set; }
        public int VotingTimeoutSeconds { get; set; }
        public int StageBreakTimeoutSeconds { get; set; }
    }
}
