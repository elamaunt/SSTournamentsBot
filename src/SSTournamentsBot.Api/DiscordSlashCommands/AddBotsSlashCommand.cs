using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.DataDomain;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class AddBotsSlashCommand : SlashCommandBase
    {
        public override string Name => "add-bots";
        public override string DescriptionKey => nameof(S.Commands_AddBots);
        
        protected readonly TournamentApi _tournamentApi;
        private volatile int _botsCounter;

        public AddBotsSlashCommand(TournamentApi tournamentApi)
        {
            _tournamentApi = tournamentApi;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var count = (long)arg.Data.Options.First(x => x.Name == "count").Value;
            
            await arg.DeferAsync();

            for (int i = 0; i < count; i++)
            {
                var race = RaceOrRandom.NewRace(GetRaceByIndex(new Random().Next(9)));
                var botId = (ulong)Interlocked.Increment(ref _botsCounter);
                await _tournamentApi.TryRegisterUser(new UserData()
                {
                    DiscordId = botId,
                    SteamId = botId,
                    Race = race,
                    StatsVerified = true
                }, "Bot_" + botId, true);
            }

            await arg.ModifyOriginalResponseAsync(x => x.Content = $"Боты зарегистрированы.");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder
                .WithDefaultPermission(true)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("count")
                    .WithDescription("Количество ботов")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer))
                .WithDMPermission(true);
        }
    }
}
