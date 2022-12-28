using SSTournamentsBot.Api.DataDomain;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class TournamentApi
    {
        private Tournament _currentTournament;
        private VotingProgress _votingProgress;

        private volatile bool _isStarted;
        private volatile bool _isCheckIn;

        private (Stage, StageBlock[])[] _stages;
        private Match[] _playedMatches;
        private Match[] _currentStageMatches;
        private HashSet<ulong> _checkInedUsers;
        private HashSet<ulong> _leftUsers;
        private Stage _initialStage;
        readonly IDrawingService _renderingService;

        readonly AsyncQueue _queue = new AsyncQueue();

        public TournamentApi(IDrawingService renderingService)
        {
            _renderingService = renderingService;
        }

        public Task<RegistrationResult> TryRegisterUser(UserData userData, string name, bool isBot = false)
        {
            return _queue.Async(() =>
            {
                if (_isStarted)
                {
                    return RegistrationResult.TournamentAlreadyStarted;
                }

                if (_currentTournament == null)
                {
                    _currentTournament = CreateTournamentByDate(Mod.Soulstorm);
                    _leftUsers = new HashSet<ulong>();
                    _checkInedUsers = new HashSet<ulong>();
                    _initialStage = null;
                    _playedMatches = new Match[0];
                    _currentStageMatches = new Match[0];
                }


                if (IsPlayerRegisteredInTournament(_currentTournament, userData.SteamId, userData.DiscordId))
                    return RegistrationResult.AlreadyRegistered;

                var player = new Player(name, userData.SteamId, userData.DiscordId, userData.Race, isBot);
                _currentTournament = RegisterPlayerInTournament(_currentTournament, player);

                return RegistrationResult.Ok;
            });
        }

        public Task DropTournament()
        {
            return _queue.Async(() =>
            {
                _currentTournament = null;
                _isCheckIn = false;
                _isStarted = false;
                _leftUsers = null;
                _checkInedUsers = null;
                _votingProgress = null;
            });
        }

        public bool IsTounamentStarted => _isStarted;
        public Player[] RegisteredPlayers => _currentTournament?.RegisteredPlayers ?? new Player[0];

        public TournamentType TournamentType => _currentTournament?.Type;

        public Match[] ActiveMatches => _currentStageMatches ?? new Match[0];

        public Task<bool> TryLeaveUser(ulong discordId, ulong steamId)
        {
            return _queue.Async(() =>
            {
                if (_currentTournament == null)
                    return false;

                if (!IsPlayerRegisteredInTournament(_currentTournament, steamId, discordId))
                    return false;

                if (_isStarted)
                {
                    if (_leftUsers.Contains(discordId))
                        return false;

                    _checkInedUsers.Remove(steamId);
                    _leftUsers.Add(discordId);

                    for (int i = 0; i < _currentStageMatches.Length; i++)
                        _currentStageMatches[i] = AddTechicalLoseToMatch(_currentStageMatches[i], steamId, TechnicalWinReason.OpponentsLeft);
                }
                else
                {
                    _currentTournament = RemovePlayerFromTournament(_currentTournament, steamId);
                }

                return true;
            });
        }

        public Task<CheckInResult> TryStartTheCheckIn()
        {
            return _queue.Async(() =>
            {
                if (_currentTournament == null)
                    return CheckInResult.NoTournament;

                if (_isCheckIn || _isStarted)
                    return CheckInResult.AlreadyStarted;

                if (!IsEnoughPlayersToPlay(_currentTournament))
                    return CheckInResult.NotEnoughPlayers;

                _isCheckIn = true;
                return CheckInResult.Done;
            });
        }

        public Task SubmitGame(FinishedGameInfo info)
        {
            return _queue.Async(() =>
            {
                if (info.GameType != GameType.Type1v1)
                    return;

                if (info.Duration < 45)
                    return;

                // TODO: other gameTypes
                if (!info.Map.IsMap1v1)
                    return;

                // TODO: other mods
                if (!info.Mod.IsMod)
                    return;

                var mod = ((ModInfo.Mod)info.Mod).Item;

                if (mod != Mod.Soulstorm)
                    return;

                var p1Info = info.Winners[0];
                var p2Info = info.Losers[0];

                if (!p1Info.Item2.IsNormalRace)
                    return;
                if (!p2Info.Item2.IsNormalRace)
                    return;

                var map = ((MapInfo.Map1v1)info.Map).Item1;

                var p1Race = ((RaceInfo.NormalRace)p1Info.Item2).Item;
                var p2Race = ((RaceInfo.NormalRace)p2Info.Item2).Item;

                var matchWithIndex = _currentStageMatches.Select((x, i) => (x, i)).FirstOrDefault(pair =>
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

                if (matchWithIndex.x == null)
                    return;

                _currentStageMatches[matchWithIndex.i] = AddWinToMatch(matchWithIndex.x, p1Info.Item1, info.ReplayLink, mod);
            });
        }

        public Task<StartResult> TryStartTheTournament()
        {
            return _queue.Async(() =>
            {
                if (!IsEnoughPlayersToPlay(_currentTournament))
                    return StartResult.NotEnoughPlayers;

                if (_isStarted)
                    return StartResult.AlreadyStarted;

                _initialStage = Start(_currentTournament);

                RegenerateStages();

                _isStarted = true;
                return StartResult.Done;
            });
        }

        private void RegenerateStages()
        {
            var stage = _initialStage;

            var stages = new List<(Stage, StageBlock[])>();
            var random = new Random(_currentTournament.Seed);

            var idCounter = 0;
            var blocks = GenerateBlocksFrom(stage, random.Next(), idCounter);

            idCounter += blocks.Count(x => x.IsMatch); // TODO: group stage

            var playedMatches = _playedMatches;

            blocks = ApplyPlayedMatches(blocks, playedMatches);
            
            var last = (stage, blocks);

            stages.Add(last);

            var currentStageMatches = new List<Match>(GetPlayableMatchesWithoutResult(blocks));

            while (!IsTerminalStage(last.Item1))
            {
                var next = GenerateNextStageFrom(last.Item1, last.Item2);

                if (!IsTerminalStage(next))
                {
                    var nextBlockes = GenerateBlocksFrom(next, random.Next(), idCounter);
                    nextBlockes = ApplyPlayedMatches(nextBlockes, playedMatches);
                    idCounter += nextBlockes.Count(x => x.IsMatch);

                    currentStageMatches.AddRange(GetPlayableMatchesWithoutResult(nextBlockes));

                    stages.Add(last = (next, nextBlockes));
                }
                else
                {
                    stages.Add(last = (next, new StageBlock[] { StageBlock.NewFree(((Stage.Brackets)next).Item[0]) }));
                }
            }

            _currentStageMatches = currentStageMatches.ToArray();
            _stages = stages.ToArray();

            ApplyLeftUsers();
        }

        private void ApplyLeftUsers()
        {
            for (int i = 0; i < _currentStageMatches.Length; i++)
            {
                var m = _currentStageMatches[i];

                var p1 = m.Player1.ValueOrDefault()?.Item1;
                var p2 = m.Player2.ValueOrDefault()?.Item1;

                if (p1 == null || p2 == null)
                    continue;

                if (_leftUsers.Contains(p1.DiscordId))
                    _currentStageMatches[i] = AddTechicalLoseToMatch(m, p1.SteamId, TechnicalWinReason.OpponentsLeft);
                else if (_leftUsers.Contains(p2.DiscordId))
                    _currentStageMatches[i] = AddTechicalLoseToMatch(m, p2.SteamId, TechnicalWinReason.OpponentsLeft);
            }
        }

        public Task<byte[]> RenderTournamentImage()
        {
            return _queue.Async(() =>
            {
                if (!_isStarted)
                    return null;

                return _renderingService.DrawToImage(_stages);
            });
        }

        public Task<Match> FindActiveMatchWith(ulong discordId)
        {
            return _queue.Async(() =>
            {
                if (!_isStarted)
                    return null;

                return _currentStageMatches.FirstOrDefault(x => x.Player1.Value.Item1.DiscordId == discordId || x.Player2.Value.Item1.DiscordId == discordId);
            });
        }

        public Task UpdatePlayersRace(UserData userData)
        {
            return _queue.Async(() =>
            {
                if (_isStarted)
                    return;

                if (_currentTournament == null)
                    return;

                var player = _currentTournament.RegisteredPlayers.First(x => x.DiscordId == userData.DiscordId);
                var updatedPlayer = new Player(player.Name, userData.SteamId, userData.DiscordId, userData.Race, player.IsBot);
                _currentTournament = UpdatePlayerInTournament(_currentTournament, updatedPlayer);
            });
        }

        public Task<CompleteStageResult> TryCompleteCurrentStage()
        {
            return _queue.Async(() =>
            {
                var matches = _currentStageMatches;

                if (!IsTounamentStarted || matches == null)
                    return CompleteStageResult.NoTournament;

                if (matches.All(x => x.Result.IsTechnicalWinner || x.Result.IsWinner))
                {
                    _playedMatches = _playedMatches.Concat(matches).ToArray();

                    return CompleteStageResult.Completed;
                }

                return CompleteStageResult.NotAllMatchesFinished;
            });
        }

        public Task<StartNextStageResult> TryStartNextStage()
        {
            return _queue.Async(() =>
            {
                if (!IsTounamentStarted)
                    return StartNextStageResult.NoTournament;

                RegenerateStages();

                if (_currentStageMatches.Length == 0)
                    return StartNextStageResult.TheStageIsTerminal;

                return StartNextStageResult.Done;
            });
        }

        public Task<UserCheckInResult> TryCheckInUser(ulong steamId)
        {
            return _queue.Async(() =>
            {
                if (!IsTounamentStarted)
                    return UserCheckInResult.NoTournament;

                if (!RegisteredPlayers.Any(x => x.SteamId == steamId))
                    return UserCheckInResult.NotRegisteredIn;

                if (!_isCheckIn)
                    return UserCheckInResult.NotCheckInStageNow;

                if (_checkInedUsers.Add(steamId))
                    return UserCheckInResult.Done;
                else
                    return UserCheckInResult.AlreadyCheckIned;
            });
        }

        public Task<AcceptVoteResult> TryAcceptVote(ulong discordId, string id)
        {
            return _queue.Async(() =>
            {
                if (_votingProgress == null || _currentTournament == null)
                    return AcceptVoteResult.NoVoting;

                if (_votingProgress.IsCompleted)
                    return AcceptVoteResult.TheVoteIsOver;

                if (_votingProgress.Voted.Any(x => x.Item1 == discordId))
                    return AcceptVoteResult.AlreadyVoted;

                if (!RegisteredPlayers.Any(x => x.DiscordId == discordId) || _leftUsers.Contains(discordId))
                    return AcceptVoteResult.YouCanNotVote;

                _votingProgress = AddVote(_votingProgress, discordId, id);

                return AcceptVoteResult.Accepted;
            });
        }

        public Task<CompleteVotingResult> TryCompleteVoting()
        {
            return _queue.Async(() =>
            {
                if (_votingProgress == null || _currentTournament == null)
                    return CompleteVotingResult.NoVoting;

                if (_votingProgress.IsCompleted)
                    return CompleteVotingResult.TheVoteIsOver;

                if (_votingProgress.VotesNeeded >= _votingProgress.Voted.Length)
                {
                    return CompleteVotingResult.CompletedPositive;
                }
                else
                {
                    return CompleteVotingResult.NoEnoughVotes;
                }
            });
        }

        public bool IsAllPlayersCheckIned()
        {
            return RegisteredPlayers.All(x => _checkInedUsers?.Contains(x.DiscordId) ?? false);
        }

        public bool IsAllActiveMatchesCompleted()
        {
            return ActiveMatches.All(x => !x.Result.IsNotCompleted);
        }
    }
}
