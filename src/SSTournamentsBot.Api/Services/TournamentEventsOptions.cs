namespace SSTournamentsBot.Api.Services
{
    public class TournamentEventsOptions
    {
        public int CallToPlayTimeoutHours { get; set; }
        public int MinimumPlayersToStartCheckin { get; set; }
        public int CheckInTimeoutMinutes { get; set; }
        public int VotingTimeoutSeconds { get; set; }
        public int StageBreakTimeoutMinutes { get; set; }
        public int StageTimeoutMinutes { get; set; }
        public int AdditionalTimeForStageMinutes { get; set; }
    }
}
