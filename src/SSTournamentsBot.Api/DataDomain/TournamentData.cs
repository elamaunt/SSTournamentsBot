using System;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.DataDomain
{
    public class TournamentData
    {
        public DateTime Date { get; set; }
        public TournamentType Type { get; set; }
        public ulong? WinnerSteamId { get; set; }
        public ulong[] PlayersSteamIds { get; set; }
        public int[] PlayersBans { get; set; }
        public MatchData[] Matches { get; set; }
        public Mod Mod { get; internal set; }
        public int TournamentId { get; internal set; }
        public int SeasonId { get; internal set; }
        public int EarnedRating { get; set; }
    }
}
