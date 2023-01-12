﻿using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using SSTournamentsBot.Api.Helpers;
using SSTournamentsBot.Api.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class PlaySlashCommand : SlashCommandBase
    {
        public override string Name => "play";
        public override string Description => "Зарегистрироваться на следующий турнир или сменить расу";

        readonly IDataService _dataService;
        readonly IStatsApi _statsApi;
        readonly TournamentApi _tournamentApi;
        readonly IEventsTimeline _timeLine;
        readonly TournamentEventsOptions _options;

        public PlaySlashCommand(IDataService dataService, IStatsApi statsApi, TournamentApi tournamentApi, IEventsTimeline timeline, IOptions<TournamentEventsOptions> options)
        {
            _dataService = dataService;
            _statsApi = statsApi;
            _tournamentApi = tournamentApi;
            _timeLine = timeline;
            _options = options.Value;
        }
        public override async Task Handle(SocketSlashCommand arg)
        {
            var user = arg.User;
            if (user == null || user.IsBot)
                return;

            var userData = _dataService.FindUserByDiscordId(arg.User.Id);

            if (userData == null)
            {
                await arg.RespondAsync("Для регистрации в системе турниров необходимо присоединить Steam аккаунт к вашему профилю. Инструкция отправлена в личные сообщения.");
                await user.SendMessageAsync(@"Для участия в ежедневных турнирах нужно подтвердить связь своей учетной записи со Steam. 
Для этого нужно сперва добавить учетную запись стим в интеграции в настройках профиля вашего Discord аккаунта. 
Сделать интеграцию видимой публично и затем перейти по ссылке ниже для подтверждения. Регистрироваться ну турнирах могут только Steam аккаунты, имеющие не менее 300 сыгранных игр всего в сервисе DowStats.");
                await user.SendMessageAsync("https://discord.com/api/oauth2/authorize?client_id=1052638908820750386&redirect_uri=http%3A%2F%2F145.239.239.58%2Fauth&response_type=code&scope=connections%20identify");
                return;
            }

            await arg.DeferAsync();

            Task Responce(string message) => arg.ModifyOriginalResponseAsync(x => x.Content = message);

            if (!userData.StatsVerified)
            {
                var stats = await _statsApi.LoadPlayerStats(userData.SteamId);

                if (stats.Games < 300)
                {
                    await Responce("Недостаточно игр на аккаунте Steam.\nРегистрироваться на турнирах могут только Steam аккаунты, имеющие не менее **300 сыгранных игр** всего в сервисе DowStats.\nВозвращайтесь, когда наиграете больше игр :)");
                    return;
                }

                userData.StatsVerified = true;
                if (!_dataService.UpdateUser(userData))
                {
                    await Responce($"Не удалось обновить данные в базе.");
                    return;
                }
            }

            var race = RaceOrRandom.RandomEveryMatch;
            var raceOption = arg.Data.Options.FirstOrDefault(x => x.Name == "race");

            if (raceOption != null)
            {
                switch ((long)raceOption.Value)
                {
                    case 0: race = RaceOrRandom.RandomEveryMatch; break;
                    case 1: race = RaceOrRandom.RandomOnTournament; break;
                    case 2: race = RaceOrRandom.NewRace(Race.SpaceMarines); break;
                    case 3: race = RaceOrRandom.NewRace(Race.Chaos); break;
                    case 4: race = RaceOrRandom.NewRace(Race.Orks); break;
                    case 5: race = RaceOrRandom.NewRace(Race.Eldar); break;
                    case 6: race = RaceOrRandom.NewRace(Race.ImperialGuard); break;
                    case 7: race = RaceOrRandom.NewRace(Race.Tau); break;
                    case 8: race = RaceOrRandom.NewRace(Race.Necrons); break;
                    case 9: race = RaceOrRandom.NewRace(Race.DarkEldar); break;
                    case 10: race = RaceOrRandom.NewRace(Race.SisterOfBattle); break;
                    default:
                        break;
                }

                userData.Race = race;

                if (!_dataService.UpdateUser(userData))
                {
                    await Responce($"Не удалось обновить данные в базе.");
                    return;
                }
            }

            var result = await _tournamentApi.TryRegisterUser(userData, user.Username);

            switch (result)
            {
                case Domain.RegistrationResult.TournamentAlreadyStarted:
                    await Responce($"Текущий турнир уже начался, изменения в данный момент невозможны.");
                    break;
                case Domain.RegistrationResult.Ok:
                    await Responce($"Вы были успешно зарегистрированы на турнир.\nАккаунт на DowStats: {userData.SteamId.BuildStatsUrl()} \nВыбранная раса: {userData.Race}");

                    if (_tournamentApi.RegisteredPlayers.Length >= _options.MinimumPlayersToStartCheckin)
                    {
                        _timeLine.RemoveAllEventsWithType(Event.StartCheckIn);
                        _timeLine.AddOneTimeEventAfterTime(Event.StartCheckIn, TimeSpan.FromSeconds(10));
                    }

                    break;
                case Domain.RegistrationResult.AlreadyRegistered:
                    if (raceOption != null)
                    {
                        var updateResult = await _tournamentApi.TryUpdatePlayersRace(userData);

                        if (!updateResult.IsCompleted)
                        {
                            var player = _tournamentApi.RegisteredPlayers.First(x => x.DiscordId == arg.User.Id);

                            await Responce($"Вы уже зарегистрированы на турнир, но смена расы в турнире на данный момент невозможна.\nАккаунт на DowStats: {userData.SteamId.BuildStatsUrl()} \nВыбранная раса: {player.Race}");
                            return;
                        }
                    }
                    await Responce($"Вы уже зарегистрированы на турнир.\nАккаунт на DowStats: {userData.SteamId.BuildStatsUrl()} \nВыбранная раса: {userData.Race}");
                    break;
                default:
                    await Responce($"Не удалось выполнить регистрацию.");
                    break;
            }
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName("race")
                .WithRequired(false)
                .AddChoice("Random every match", 0)
                .AddChoice("Random on the tournament", 1)
                .AddChoice("Space Marines", 2)
                .AddChoice("Chaos Space Marines", 3)
                .AddChoice("Orks", 4)
                .AddChoice("Eldar", 5)
                .AddChoice("Imperial Guard", 6)
                .AddChoice("Tau", 7)
                .AddChoice("Necron", 8)
                .AddChoice("Dark Eldar", 9)
                .AddChoice("Sisters Of Battle", 10)
                .WithType(ApplicationCommandOptionType.Integer)
                .WithDescription("Выбрать расу"));
        }
    }
}
