using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.DataDomain
{
    public class MatchData
    {
        public ulong PlayerSteamId1 { get; set; }
        public ulong PlayerSteamId2 { get; set; }
        public MatchResult Result { get; set; }
        public Replay[] Replays { get; set; }
    }
}