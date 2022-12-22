using Microsoft.FSharp.Core;
using SSTournamentsBot.Api.DataDomain;
using SSTournamentsBot.Api.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class TournamentApi
    {
        private Tournament _currentTournament;
        private List<(Stage, MatchOrFreeBlock[])> _stages;

        private bool _isStarted;
        private bool _isCheckIn;
        private List<UserData> _checkInedUsers;
        private List<UserData> _leftUsers;

        readonly IDrawingService _renderingService;

        public TournamentApi(IDrawingService renderingService)
        {
            _renderingService = renderingService;
        }

        public RegistrationResult TryRegisterUser(UserData userData, string name, bool isBot = false)
        {
            if (_isStarted)
            {
                return RegistrationResult.TournamentAlreadyStarted;
            }

            if (_currentTournament == null)
            {
                _currentTournament = CreateTournamentByDate(Mod.Soulstorm);
                _leftUsers = new List<UserData>();
                _checkInedUsers = new List<UserData>();
                _stages = new List<(Stage, MatchOrFreeBlock[])>();
            }


            if (IsPlayerRegisteredInTournament(_currentTournament, userData.SteamId, userData.DiscordId))
                return RegistrationResult.AlreadyRegistered;

            var player = new Player(name, userData.SteamId, userData.DiscordId, userData.Race, isBot);
            _currentTournament = RegisterPlayerInTournament(_currentTournament, player);

            return RegistrationResult.Ok;
        }

        public void DropTournament()
        {
            _currentTournament = null;
            _isCheckIn = false;
            _isStarted = false;
            _leftUsers = null;
            _checkInedUsers = null;
        }

        public bool IsTounamentStarted => _isStarted;
        public Player[] RegisteredPlayers => _currentTournament?.RegisteredPlayers ?? new Player[0];

        public TournamentType TournamentType => _currentTournament?.Type;

        public bool TryLeaveUser(UserData userData)
        {
            if (_currentTournament == null)
                return false;

            if (!IsPlayerRegisteredInTournament(_currentTournament, userData.SteamId, userData.DiscordId))
                return false;

             _currentTournament = RemovePlayerFromTournament(_currentTournament, userData.SteamId);
            return true;
        }

        public CheckInResult TryStartTheCheckIn()
        {
            if (_currentTournament == null)
                return CheckInResult.NoTournament;

            if (_isCheckIn || _isStarted)
                return CheckInResult.AlreadyStarted;

            if (!IsEnoughPlayersToPlay(_currentTournament))
                return CheckInResult.NotEnoughPlayers;

            _isCheckIn = true;
            return CheckInResult.Done;
        }

        public StartResult TryStartTheTournament()
        {
            if (!IsEnoughPlayersToPlay(_currentTournament))
                return StartResult.NotEnoughPlayers;

            if (_isStarted)
                return StartResult.AlreadyStarted;

            var stage = Start(_currentTournament);
            var random = new Random(_currentTournament.Seed);
            var matches = GenerateMatchesfrom(stage, random.Next(), true);

            _stages.Add((stage, matches));
            _isStarted = true;
            return StartResult.Done;
        }

        public byte[] RenderTournamentImage()
        {
            if (!_isStarted)
                return null;

            var stages = new List<(Stage, MatchOrFreeBlock[])>(_stages);
            var last = stages.Last();

            var isFirstStage = stages.Count == 1;

            while (!IsTerminalStage(last.Item1))
            {
                var next = GenerateNextStageFrom(last.Item1, GetMatches(last.Item2), isFirstStage);
                isFirstStage = false;

                if (!IsTerminalStage(next))
                {
                    var matches = GenerateMatchesfrom(next, 0, false);
                    stages.Add(last = (next, matches));
                }
                else
                {
                    stages.Add(last = (next, new MatchOrFreeBlock[0]));
                }
            }

            return _renderingService.DrawToImage(stages.ToArray());
        }

        public Match FindMatchWith(ulong discordId)
        {
            if (!_isStarted)
                return null;

            var stages = new List<(Stage, MatchOrFreeBlock[])>(_stages);

            var last = stages.Last();

            return GetMatches(last.Item2).FirstOrDefault(x =>
            {
                if (FSharpOption<Tuple<Player, Race>>.get_IsSome(x.Player1) && x.Player1.Value.Item1.DiscordId == discordId)
                {
                    return true;
                }

                if (FSharpOption<Tuple<Player, Race>>.get_IsSome(x.Player2) && x.Player2.Value.Item1.DiscordId == discordId)
                {
                    return true;
                }

                return false;
            });
        }

        public void UpdatePlayersRace(UserData userData)
        {
            if (_isStarted)
                return;

            if (_currentTournament == null)
                return;

            var player = _currentTournament.RegisteredPlayers.First(x => x.DiscordId == userData.DiscordId);
            var updatedPlayer = new Player(player.Name, userData.SteamId, userData.DiscordId, userData.Race, player.IsBot);
            _currentTournament = UpdatePlayerInTournament(_currentTournament, updatedPlayer);
        }

        public CompleteStageResult TryCompleteCurrentStage()
        {
            throw new NotImplementedException();
        }

        public StartNextStageResult TryStartNexttage()
        {
            throw new NotImplementedException();
        }

        public UserCheckInResult ChechInUser(UserData userData)
        {
            return UserCheckInResult.Done;
        }
    }
}
