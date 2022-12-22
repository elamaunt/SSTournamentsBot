using System;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.DataDomain
{
    public class MatchData
    {
        public int Stage { get; set; }
        public int? Group { get; set; }
        public ulong PlayerSteamId1 { get; set; }
        public ulong PlayerSteamId2 { get; set; }
        public Tuple<int, int> Count { get; set; }
        public MatchResult Result { get; set; }
        public Replay Replay { get; set; }
        public Map Map { get; set; }
        public Race Race1 { get; set; }
        public Race Race2 { get; set; }
    }
}