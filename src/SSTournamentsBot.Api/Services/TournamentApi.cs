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
        private bool _isCheckInStage;
        private bool _stageCompleted;

        private (Stage, StageBlock[])[] _stages;
        private Match[] _playedMatches;
        private Match[] _currentStageMatches;
        private HashSet<ulong> _checkInedUsers;
        private Dictionary<ulong, TechnicalWinReason> _excludedUsers;
        private Dictionary<ulong, DateTime> _playerRegistrationTime;
        private Stage _initialStage;

        readonly IDataService _dataService;
        readonly IDrawingService _renderingService;

        readonly AsyncQueue _queue = new AsyncQueue();

        public Mod Mod { get; set; } = Mod.Soulstorm;

        public TournamentApi(IDrawingService renderingService, IDataService dataService)
        {
            _renderingService = renderingService;
            _dataService = dataService;
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
                    var (SeasonId, TournamentId) = _dataService.GetCurrentTournamentIds();

                    _currentTournament = CreateTournament(Mod, SeasonId, TournamentId);
                    _excludedUsers = new Dictionary<ulong, TechnicalWinReason>();
                    _playerRegistrationTime = new Dictionary<ulong, DateTime>();
                    _checkInedUsers = new HashSet<ulong>();
                    _initialStage = null;
                    _playedMatches = new Match[0];
                    _currentStageMatches = new Match[0];
                    TimeAlreadyExtended = false;
                    SingleMatchTimeAlreadyExtended = false;
                }

                if (IsPlayerRegisteredInTournament(_currentTournament, userData.SteamId, userData.DiscordId))
                    return RegistrationResult.AlreadyRegistered;

                var player = new Player(name, userData.SteamId, userData.DiscordId, userData.Race, isBot, userData.Map1v1Bans, _currentTournament.Seed ^ userData.DiscordId.GetHashCode());
                _currentTournament = RegisterPlayerInTournament(_currentTournament, player);
                _playerRegistrationTime.Remove(userData.DiscordId);

                if (_isCheckInStage)
                {
                    _checkInedUsers.Add(player.SteamId);
                    return RegistrationResult.RegisteredAndCheckIned;
                }

                return RegistrationResult.Registered;
            });
        }

        public Task DropTournament()
        {
            return _queue.Async(() =>
            {
                _currentTournament = null;
                _isCheckInStage = false;
                _isStarted = false;
                _excludedUsers = null;
                _checkInedUsers = null;
                _votingProgress = null;
                TimeAlreadyExtended = false;
                SingleMatchTimeAlreadyExtended = false;
                _stageCompleted = false;
                _currentStageMatches = null;
                _playedMatches = null;
            });
        }

        public bool IsTournamentStarted => _isStarted;
        public Player[] RegisteredPlayers => _currentTournament?.RegisteredPlayers ?? new Player[0];
        private IEnumerable<Player> ActivePlayersEnumerable => IsTournamentStarted ? RegisteredPlayers
            .Where(x => !_excludedUsers.ContainsKey(x.DiscordId))
            .Where(x => !_playedMatches.Any(m => IsLoseOf(m, x)))
            .Where(x => !ActiveMatches.Any(m => IsLoseOf(m, x))) : Enumerable.Empty<Player>();

        public Player[] ActivePlayers => ActivePlayersEnumerable.ToArray();
        public bool IsAllPlayersCheckIned => RegisteredPlayers.All(x => _checkInedUsers?.Contains(x.SteamId) ?? false);
        public bool IsAllActiveMatchesCompleted => ActiveMatches.All(x => !x.Result.IsNotCompleted);
        public Player[] CheckInedPlayers => RegisteredPlayers.Where(x => _checkInedUsers?.Contains(x.SteamId) ?? false).ToArray();
        public TournamentType TournamentType => _currentTournament?.Type ?? TournamentType.Regular;
        public Match[] ActiveMatches => _currentStageMatches ?? new Match[0];
        public Match[] PlayedMatches => _playedMatches ?? new Match[0];
        public VotingProgress VotingProgress => _votingProgress;
        public bool SingleMatchTimeAlreadyExtended { get; set; }
        public bool TimeAlreadyExtended { get; set; }
        public DateTime? StartDate => _currentTournament?.StartDate.AsNullable();
        public int PossibleNextStageMatches => ActivePlayersEnumerable.Count() / 2;
        public bool IsCheckinStage => _isCheckInStage;
        public int Id => _currentTournament?.Id ?? 0;
        public string Header => $"{TournamentType} {_currentTournament?.Mod} AutoCup {Id} | {StartDate.Value.PrettyShortDatePrint()}";

        public Task<LeaveUserResult> TryLeaveUser(ulong discordId, ulong steamId, TechnicalWinReason reason)
        {
            return _queue.Async(() =>
            {
                if (_currentTournament == null)
                    return LeaveUserResult.NoTournament;

                if (!IsPlayerRegisteredInTournament(_currentTournament, steamId, discordId))
                    return LeaveUserResult.NotRegistered;

                if (_isStarted)
                {
                    if (_excludedUsers.ContainsKey(discordId))
                        return LeaveUserResult.NewAlreadyLeftBy(_excludedUsers[discordId]);

                    _checkInedUsers.Remove(steamId);
                    _excludedUsers.Add(discordId, reason);

                    for (int i = 0; i < _currentStageMatches.Length; i++)
                        _currentStageMatches[i] = AddTechicalLoseToMatch(_currentStageMatches[i], steamId, reason);
                }
                else
                {
                    _playerRegistrationTime.Remove(discordId);
                    _currentTournament = RemovePlayerFromTournament(_currentTournament, steamId);
                }

                return LeaveUserResult.Done;
            });
        }

        public Task<CheckInResult> TryStartTheCheckIn()
        {
            return _queue.Async(() =>
            {
                if (_currentTournament == null)
                    return CheckInResult.NoTournament;

                if (_isCheckInStage || _isStarted)
                    return CheckInResult.AlreadyStarted;

                if (!IsEnoughPlayersToPlay(_currentTournament))
                    return CheckInResult.NotEnoughPlayers;

                _isCheckInStage = true;
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

                var match = matchWithIndex.x;

                if (match == null)
                    return SubmitGameResult.MatchNotFound;
               
                if (_stageCompleted)
                {
                    if (match.Result.IsTechnicalWinner && ((MatchResult.TechnicalWinner)match.Result).Item2.IsVoting)
                    {
                        _excludedUsers.Remove(match.Player1.Value.Item1.DiscordId);
                        _excludedUsers.Remove(match.Player2.Value.Item1.DiscordId);

                        _currentStageMatches[matchWithIndex.i] = ForceAddWinToMatch(match, p1Info.Item1, info.ReplayLink);
                        return SubmitGameResult.Completed;
                    }
                    else
                        return SubmitGameResult.MatchNotFound;
                }
                else
                {
                    _currentStageMatches[matchWithIndex.i] = AddWinToMatch(match, p1Info.Item1, info.ReplayLink);

                    if (_currentStageMatches.All(x => !x.Result.IsNotCompleted))
                        return SubmitGameResult.CompletedAndFinishedTheStage;
                }

                return SubmitGameResult.Completed;
            });
        }

        public Task<DateTime> GetPlayerRegisterTime(ulong discordId)
        {
            return _queue.Async(() =>
            {
                return _playerRegistrationTime.GetValueOrDefault(discordId, GetMoscowTime());
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

                RegenerateStagesAndCurrentStageMatches();

                _isStarted = true;
                return StartResult.Done;
            });
        }

        private void RegenerateStagesAndCurrentStageMatches()
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
                    nextBlockes = ApplyExcludedUsers(ApplyPlayedMatches(nextBlockes, playedMatches), _excludedUsers);
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

        public Task<UpdatePlayerResult> TryUpdatePlayer(UserData userData)
        {
            return _queue.Async(() =>
            {
                if (_isStarted)
                    return UpdatePlayerResult.NotPossible;

                if (_currentTournament == null)
                    return UpdatePlayerResult.NoTournament;

                var player = _currentTournament.RegisteredPlayers.FirstOrDefault(x => x.DiscordId == userData.DiscordId);

                if (player == null)
                    return UpdatePlayerResult.NotRegistered;

                var updatedPlayer = new Player(player.Name, userData.SteamId, userData.DiscordId, userData.Race, player.IsBot, userData.Map1v1Bans, _currentTournament.Seed ^ userData.DiscordId.GetHashCode());
                _currentTournament = UpdatePlayerInTournament(_currentTournament, updatedPlayer);

                return UpdatePlayerResult.Completed;
            });
        }

        public Task<CompleteStageResult> TryCompleteCurrentStage()
        {
            return _queue.Async(() =>
            {
                var matches = _currentStageMatches;

                if (!IsTournamentStarted || matches == null)
                    return CompleteStageResult.NoTournament;

                if (_stageCompleted)
                    return CompleteStageResult.NoUncompletedStage;

                if (matches.All(x => x.Result.IsTechnicalWinner || x.Result.IsWinner))
                {
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
                if (!IsTournamentStarted)
                    return StartNextStageResult.NoTournament;

                if (!_stageCompleted)
                    return StartNextStageResult.PreviousStageIsNotCompleted;

                _playedMatches = _playedMatches.Concat(_currentStageMatches).ToArray();
                _stageCompleted = false;
                RegenerateStagesAndCurrentStageMatches();

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

                if (!_isCheckInStage)
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

                if (role == GuildRole.Everyone && !CanUserVote(discordId))
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

        private bool CanUserVote(ulong discordId)
        {
            if (!RegisteredPlayers.Any(x => x.DiscordId == discordId))
                return false;

            if (_excludedUsers == null)
                return false;

            if (_excludedUsers.TryGetValue(discordId, out var reason))
            {
                if (reason.IsVoting)
                    return true;
                if (reason.IsOpponentsLeft)
                    return false;
                if (reason.IsOpponentsKicked)
                    return false;
                if (reason.IsOpponentsBan)
                    return false;
            }

            return true;
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
    }
}
