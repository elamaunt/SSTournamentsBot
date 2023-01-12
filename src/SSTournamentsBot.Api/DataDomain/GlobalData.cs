namespace SSTournamentsBot.Api.DataDomain
{
    public class GlobalData
    {
        public int CurrentSeasonId { get; set; } = 1;
        public int CurrentTournamentId { get; set; } = 1;
        public int MatchesPlayed { get; set; } = 0;
        public int EarnedRating { get; set; } = 0;
    }
}
