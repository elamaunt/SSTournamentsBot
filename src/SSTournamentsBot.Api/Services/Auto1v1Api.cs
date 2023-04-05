using Microsoft.FSharp.Core;
using SSTournamentsBot.Api.DataDomain;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.AutoDomain;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class Auto1v1Api
    {
        private VotingProgress _votingProgress;

        readonly AsyncQueue _queue = new AsyncQueue();

        public Mod Mod { get; set; } = Mod.Soulstorm;

        readonly Dictionary<ulong, Player> _playersPool = new Dictionary<ulong, Player>();
        readonly Random _random = new Random();
        readonly List<Match> _activeMatches = new List<Match>();
        readonly Dictionary<Match, DateTime> _matchesTime = new Dictionary<Match, DateTime>();

        public Task<AutoRegistrationResult> TryRegisterUser(UserData userData, string name, bool isBot = false)
        {
            return _queue.Async(() =>
            {
                if (_playersPool.ContainsKey(userData.DiscordId))
                    return AutoRegistrationResult.AlreadyRegistered;

                if (IsUserHasActiveMatch(userData.DiscordId))
                    return AutoRegistrationResult.AlreadyHaveMatch;

                var player = new Player(name, userData.SteamId, userData.DiscordId, userData.Race, isBot, userData.Map1v1Bans, _random.Next());

                _playersPool.Add(userData.DiscordId, player);
                return AutoRegistrationResult.Registered;
            });
        }

        private bool IsUserHasActiveMatch(ulong discordId)
        {
            return _activeMatches.Any(x => x.Player1.ValueOrDefault()?.Item1.DiscordId == discordId || x.Player2.ValueOrDefault()?.Item1.DiscordId == discordId);
        }

        public Task<SubmitGameResult> TrySubmitGame(FinishedGameInfo info)
        {
            return _queue.Async(() =>
            {
                if (info.GameType != GameType.Type1v1)
                    return SubmitGameResult.MatchNotFound;

                if (info.Duration < 45)
                    return SubmitGameResult.TooShortDuration;

                // TODO: other gameTypes
                if (!info.Map.IsMap1v1)
                    return SubmitGameResult.DifferentMap;

                // TODO: other mods
                if (!info.UsedMod.IsMod)
                    return SubmitGameResult.DifferentMod;

                var mod = ((ModInfo.Mod)info.UsedMod).Item;

                if (mod != Mod)
                    return SubmitGameResult.DifferentMod;

                var p1Info = info.Winners[0];
                var p2Info = info.Losers[0];

                if (!p1Info.Item2.IsNormalRace)
                    return SubmitGameResult.DifferentRace;
                if (!p2Info.Item2.IsNormalRace)
                    return SubmitGameResult.DifferentRace;

                var map = ((MapInfo.Map1v1)info.Map).Item1;

                var p1Race = ((RaceInfo.NormalRace)p1Info.Item2).Item;
                var p2Race = ((RaceInfo.NormalRace)p2Info.Item2).Item;

                var matchWithIndex = _activeMatches.Select((x, i) => (x, i)).FirstOrDefault(pair =>
                {
                    var x = pair.x;
                    if (x.Map != map)
                        return false;

                    return (x.Player1.Value.Item1.SteamId == p1Info.Item1 &&
                        x.Player2.Value.Item1.SteamId == p2Info.Item1 &&
                        x.Player1.Value.Item2 == p1Race &&
                        x.Player2.Value.Item2 == p2Race) ||

                        (x.Player1.Value.Item1.SteamId == p2Info.Item1 &&
                        x.Player2.Value.Item1.SteamId == p1Info.Item1 &&
                        x.Player1.Value.Item2 == p2Race &&
                        x.Player2.Value.Item2 == p1Race);
                });

                var match = matchWithIndex.x;

                if (match == null)
                    return SubmitGameResult.MatchNotFound;

                return SubmitGameResult.Completed;
            });
        }

        public Task<bool> TryLeaveUser(ulong discordId)
        {
            return _queue.Async(() =>
            {
                if (_playersPool.Remove(discordId))
                    return true;
                return false;
            });
        }

        public Task DropGeneration()
        {
            return _queue.Async(() =>
            {
                _playersPool.Clear();
            });
        }

        public Task<Match[]> GenerateMatches()
        {
            return _queue.Async(() =>
            {
                var players = Shuffle(_random.Next(), _playersPool.Values.Where(x => !IsUserHasActiveMatch(x.DiscordId)).ToArray());

                var matches = new List<Match>();
                var time = GetMoscowTime();
                var count = players.Length / 2;

                for (int i = 0; i < count; i++)
                {
                    var p1 = players[i * 2];
                    var p2 = players[i * 2+1];

                    var match = CreateMatch(i, p1, p2, _random);
                    matches.Add(match);
                    _activeMatches.Add(match);
                    _matchesTime.Add(match, time);
                }

                return matches.ToArray();
            });
        }

        public Task<Match[]> GetExpiredMatches()
        {
            return _queue.Async(() =>
            {
                var time = GetMoscowTime();
                return _activeMatches.Where(x => (time - _matchesTime[x]).TotalHours > 1.0).ToArray();
            });
        }

        public Task<StartVotingResult> TryStartVoting(Voting voting)
        {
            return _queue.Async(() =>
            {
                if (_votingProgress != null)
                    return (StartVotingResult.AlreadyHasVoting);

                _votingProgress = InitVotingProgress(voting);
                return StartVotingResult.Completed;
            });
        }

        public Task<AcceptVoteResult> TryAcceptVote(ulong discordId, int id, GuildRole role)
        {
            return _queue.Async(() =>
            {
                if (_votingProgress == null)
                    return AcceptVoteResult.NoVoting;

                if (!_votingProgress.State.IsNotCompleted)
                    return AcceptVoteResult.TheVoteIsOver;

                if (_votingProgress.Voted.Any(x => x.Item1 == discordId))
                    return AcceptVoteResult.AlreadyVoted;

                if (role == GuildRole.Everyone || !_votingProgress.Voting.AdminForcingEnabled)
                {
                    _votingProgress = AddVote(_votingProgress, discordId, id);
                    return AcceptVoteResult.Accepted;
                }
                else
                {
                    _votingProgress = ForceCompleteVote(_votingProgress, id);
                    CompleteVotingAndHandleResult();
                    return AcceptVoteResult.CompletedByThisVote;
                }
            });
        }

        public Task<CompleteVotingResult> TryCompleteVoting()
        {
            return _queue.Async(() =>
            {
                if (_votingProgress == null)
                    return (CompleteVotingResult.NoVoting);

                if (!_votingProgress.State.IsNotCompleted)
                    return (CompleteVotingResult.TheVoteIsOver);
                CompleteVotingAndHandleResult();
                return CompleteVotingResult.Completed;
            });
        }

        private void CompleteVotingAndHandleResult()
        {
            var progress = CompleteVote(_votingProgress);

            var handler = progress.Voting.Handler;
            SwitchVotingResult(progress.State, FSharpFunc<Unit, Unit>.FromConverter(x =>
            {
                handler.Invoke(FSharpOption<int>.None);
                return SharedUnit;
            }), FSharpFunc<Unit, Unit>.FromConverter(x =>
            {
                handler.Invoke(FSharpOption<int>.None);
                return SharedUnit;
            }), FSharpFunc<int, Unit>.FromConverter(x =>
            {
                handler.Invoke(FSharpOption<int>.Some(x));
                return SharedUnit;
            }));

            _votingProgress = null;
        }
    }
}
