using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using SSTournamentsBot.Api.Helpers;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System;
using System.Globalization;
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
        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var user = arg.User;
            if (user == null || user.IsBot)
                return;

            var userData = _dataService.FindUserByDiscordId(arg.User.Id);

            if (userData == null)
            {
                await arg.RespondAsync(OfKey(nameof(S.Play_RegisterIntroMessage)).Build(culture));
                await user.SendMessageAsync(OfKey(nameof(S.Play_InstructionsMessage)).Build(culture));
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
                    await Responce(OfKey(nameof(S.Play_NotEnoughGames)).Build(culture));
                    return;
                }

                userData.StatsVerified = true;
                if (!_dataService.UpdateUser(userData))
                {
                    await Responce(OfKey(nameof(S.Bot_ImposibleToUpdateDataBase)).Build(culture));
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
                    await Responce(OfKey(nameof(S.Bot_ImposibleToUpdateDataBase)).Build(culture));
                    return;
                }
            }

            var result = await _tournamentApi.TryRegisterUser(userData, user.Username);

            switch (result)
            {
                case Domain.RegistrationResult.TournamentAlreadyStarted:
                    await Responce(OfKey(nameof(S.Play_ActivityStarted)).Build(culture));
                    break;
                    
                case Domain.RegistrationResult.Registered:
                case Domain.RegistrationResult.RegisteredAndCheckIned:

                    if (result == Domain.RegistrationResult.Registered)
                    {
                        await Responce(OfKey(nameof(S.Play_Successfull)).Format(userData.SteamId.BuildStatsUrl(), userData.Race).Build(culture));

                        if (_tournamentApi.RegisteredPlayers.Length >= _options.MinimumPlayersToStartCheckin)
                        {
                            _timeLine.RemoveAllEventsWithType(Event.NewStartCheckIn(context.Name));
                            _timeLine.AddOneTimeEventAfterTime(Event.NewStartCheckIn(context.Name), TimeSpan.FromSeconds(10));
                        }
                    }
                    else
                    {
                        await Responce(OfKey(nameof(S.Play_SuccessfullAndChekined)).Format(userData.SteamId.BuildStatsUrl(), userData.Race).Build(culture));
                    }

                    break;
                case Domain.RegistrationResult.AlreadyRegistered:
                    if (raceOption != null)
                    {
                        var updateResult = await _tournamentApi.TryUpdatePlayer(userData);

                        if (!updateResult.IsCompleted)
                        {
                            var player = _tournamentApi.RegisteredPlayers.First(x => x.DiscordId == arg.User.Id);

                            await Responce(OfKey(nameof(S.Play_AlreadyRegisteredButNoChanges)).Format(userData.SteamId.BuildStatsUrl(), player.Race).Build(culture));
                            return;
                        }
                    }
                    await Responce(OfKey(nameof(S.Play_AlreadyRegistered)).Format(userData.SteamId.BuildStatsUrl(), userData.Race).Build(culture));
                    break;
                default:
                    await Responce(OfKey(nameof(S.Play_Imposible)).Build(culture));
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
