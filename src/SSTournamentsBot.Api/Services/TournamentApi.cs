using Microsoft.FSharp.Core;
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
        private volatile bool _stageCompleted;

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
                    TimeAlreadyExtended = false;
                    SingleMatchTimeAlreadyExtended = false;
                }

                if (IsPlayerRegisteredInTournament(_currentTournament, userData.SteamId, userData.DiscordId))
                    return RegistrationResult.AlreadyRegistered;

                var player = new Player(name, userData.SteamId, userData.DiscordId, userData.Race, isBot, _currentTournament.Seed ^ userData.DiscordId.GetHashCode());
                _currentTournament = RegisterPlayerInTournament(_currentTournament, player);

                if (_isCheckIn)
                    _checkInedUsers.Add(player.SteamId);

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
                TimeAlreadyExtended = false;
                SingleMatchTimeAlreadyExtended = false;
                _stageCompleted = false;
            });
        }

        public bool IsTounamentStarted => _isStarted;
        public Player[] RegisteredPlayers => _currentTournament?.RegisteredPlayers ?? new Player[0];
        public TournamentType TournamentType => _currentTournament?.Type;
        public Match[] ActiveMatches => _currentStageMatches ?? new Match[0];
        public Match[] PlayedMatches => _playedMatches ?? new Match[0];
        public VotingProgress VotingProgress => _votingProgress;

        public bool SingleMatchTimeAlreadyExtended { get; set; }
        public bool TimeAlreadyExtended { get; set; }
        public DateTime Date => _currentTournament?.Date ?? DateTime.Today;

        public int PossibleNextStageMatches => RegisteredPlayers
            .Where(x => !_leftUsers.Contains(x.DiscordId))
            .Where(x => !_playedMatches.Any(m => IsLoseOf(m, x)))
            .Where(x => !ActiveMatches.Any(m => IsLoseOf(m, x)))
            .Count() / 2;

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

        public Task<SubmitGameResult> TrySubmitGame(FinishedGameInfo info)
        {
            return _queue.Async(() =>
            {
                if (_currentTournament == null)
                    return SubmitGameResult.NoTournament;

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

                if (mod != Mod.Soulstorm)
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
                    return SubmitGameResult.MatchNotFound;

                _currentStageMatches[matchWithIndex.i] = AddWinToMatch(matchWithIndex.x, p1Info.Item1, info.ReplayLink);

                if (_currentStageMatches.All(x => !x.Result.IsNotCompleted))
                    return SubmitGameResult.CompletedAndFinishedTheStage;

                return SubmitGameResult.Completed;
            });
        }

        public Task<StartResult> TryStartTheTournament()
        {
            return _queue.Async(() =>
            {
                if (_currentTournament == null)
                    return StartResult.NoTournament;

                _currentTournament = RemovePlayersInTournamentThanSteamIdNotContainsIn(_currentTournament, _checkInedUsers.ToArray());

                if (!IsEnoughPlayersToPlay(_currentTournament))
                    return StartResult.NotEnoughPlayers;

                if (_isStarted)
                    return StartResult.AlreadyStarted;

                _currentTournament = SetStartDate(_currentTournament);
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

        public Task<TournamentBundle> BuildAllData()
        {
            return _queue.Async(() =>
            {
                var winner = ((StageBlock.Free)_stages.Last().Item2[0]).Item;
                return new TournamentBundle(_currentTournament, _playedMatches, winner, _renderingService.DrawToImage(_currentTournament, _stages));
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

                return _renderingService.DrawToImage(_currentTournament, _stages);
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

        public Task<UpdatePlayersRaceResult> UpdatePlayersRace(UserData userData)
        {
            return _queue.Async(() =>
            {
                if (_isStarted)
                    return UpdatePlayersRaceResult.NotPossible;

                if (_currentTournament == null)
                    return UpdatePlayersRaceResult.NoTournament;

                var player = _currentTournament.RegisteredPlayers.FirstOrDefault(x => x.DiscordId == userData.DiscordId);

                if (player == null)
                    return UpdatePlayersRaceResult.NotRegistered;

                var updatedPlayer = new Player(player.Name, userData.SteamId, userData.DiscordId, userData.Race, player.IsBot, _currentTournament.Seed ^ userData.DiscordId.GetHashCode());
                _currentTournament = UpdatePlayerInTournament(_currentTournament, updatedPlayer);

                return UpdatePlayersRaceResult.Completed;
            });
        }

        public Task<CompleteStageResult> TryCompleteCurrentStage()
        {
            return _queue.Async(() =>
            {
                var matches = _currentStageMatches;

                if (!IsTounamentStarted || matches == null)
                    return CompleteStageResult.NoTournament;

                if (_stageCompleted)
                    return CompleteStageResult.NoUncompletedStage;

                if (matches.All(x => x.Result.IsTechnicalWinner || x.Result.IsWinner))
                {
                    _playedMatches = _playedMatches.Concat(matches).ToArray();
                    _stageCompleted = true;
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

                if (!_stageCompleted)
                    return StartNextStageResult.PreviousStageIsNotCompleted;

                _stageCompleted = false;
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
                if (_currentTournament == null)
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

                if (role == GuildRole.Everyone && !RegisteredPlayers.Any(x => x.DiscordId == discordId) || (_leftUsers?.Contains(discordId) ?? false))
                    return AcceptVoteResult.YouCanNotVote;

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
                if (_votingProgress == null || _currentTournament == null)
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

        public bool IsAllPlayersCheckIned()
        {
            return RegisteredPlayers.All(x => _checkInedUsers?.Contains(x.SteamId) ?? false);
        }

        public bool IsAllActiveMatchesCompleted()
        {
            return ActiveMatches.All(x => !x.Result.IsNotCompleted);
        }
    }
}
