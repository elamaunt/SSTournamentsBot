using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class PlaySlashCommand : SlashCommandBase
    {
        public override string Name => "play";
        public override string Description => "Зарегистрироваться на следующий турнир или сменить расу";

        readonly IDataService _dataService;
        readonly IStatsApi _statsApi;
        readonly TournamentApi _tournamentApi;
        public PlaySlashCommand(IDataService dataService, IStatsApi statsApi, TournamentApi tournamentApi)
        {
            _dataService = dataService;
            _statsApi = statsApi;
            _tournamentApi = tournamentApi;
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
                await user.SendMessageAsync("https://discord.com/api/oauth2/authorize?client_id=1052638908820750386&redirect_uri=http%3A%2F%2F127.0.0.1:2272%2Fauth&response_type=code&scope=connections%20identify");
                return;
            }

            var deffered = !userData.StatsVerified;

            if (!userData.StatsVerified)
            {
                await arg.DeferAsync();
                var stats = await _statsApi.LoadPlayerStats(userData.SteamId);

                if (stats.Games < 300)
                {
                    await arg.RespondAsync("Недостаточно игр на аккаунте Steam.\nРегистрироваться ну турнирах могут только Steam аккаунты, имеющие не менее 300 сыгранных игр всего в сервисе DowStats.\nВозвращайтесь, когда наиграете больше игр :)");
                    return;
                }

                userData.StatsVerified = true;
                userData = _dataService.UpdateUser(userData);
            }

            var race = RaceOrRandom.Random;
            var raceOption = arg.Data.Options.FirstOrDefault(x => x.Name == "race");

            if (raceOption != null)
            {
                switch ((long)raceOption.Value)
                {
                    case 0: race = RaceOrRandom.Random; break;
                    case 1: race = RaceOrRandom.NewRace(Race.SpaceMarines); break;
                    case 2: race = RaceOrRandom.NewRace(Race.Chaos); break;
                    case 3: race = RaceOrRandom.NewRace(Race.Orks); break;
                    case 4: race = RaceOrRandom.NewRace(Race.Eldar); break;
                    case 5: race = RaceOrRandom.NewRace(Race.ImperialGuard); break;
                    case 6: race = RaceOrRandom.NewRace(Race.Tau); break;
                    case 7: race = RaceOrRandom.NewRace(Race.Necrons); break;
                    case 8: race = RaceOrRandom.NewRace(Race.DarkEldar); break;
                    case 9: race = RaceOrRandom.NewRace(Race.SisterOfBattle); break;
                    default:
                        break;
                }

                userData.Race = race;
                userData = _dataService.UpdateUser(userData);
            }

            var result = _tournamentApi.TryRegisterUser(userData, user.Username);

            async Task Responce(string message)
            {
                if (deffered)
                    await arg.ModifyOriginalResponseAsync(x => { x.Content = new Optional<string>(message); });
                else
                    await arg.RespondAsync(message);
            }

            switch (result)
            {
                case Domain.RegistrationResult.TournamentAlreadyStarted:
                    await Responce($"Текущий турнир уже начался, изменения в данный момент невозможны.");
                    break;
                case Domain.RegistrationResult.Ok:
                    await Responce($"Вы были успешно зарегистрированы на турнир.\nАккаунт на DowStats: https://dowstats.ru/player.php?sid={userData.SteamId}&server=steam#tab0 \nВыбранная раса: {userData.Race}");
                    break;
                case Domain.RegistrationResult.AlreadyRegistered:
                    if (raceOption != null)
                        _tournamentApi.UpdatePlayersRace(userData);
                    await Responce($"Вы уже зарегистрированы на турнир.\nАккаунт на DowStats: https://dowstats.ru/player.php?sid={userData.SteamId}&server=steam#tab0 \nВыбранная раса: {userData.Race}");
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
                .AddChoice("Random", 0)
                .AddChoice("Space Marines", 1)
                .AddChoice("Chaos Space Marines", 2)
                .AddChoice("Orks", 3)
                .AddChoice("Eldar", 4)
                .AddChoice("Imperial Guard", 5)
                .AddChoice("Tau", 6)
                .AddChoice("Necron", 7)
                .AddChoice("Dark Eldar", 8)
                .AddChoice("Sisters Of Battle", 9)
                .WithType(ApplicationCommandOptionType.Integer)
                .WithDescription("Выбрать расу"));
        }
    }
}
