using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SSTournamentsBot.Api.DiscordSlashCommands;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class DiscordCommandsHandler : IHostedService
    {
        readonly DiscordSocketClient _client;
        readonly IContextService _contextService;
        readonly DiscordBotOptions _options;
        readonly TournamentEventsOptions _tournamentOptions;
        readonly Dictionary<string, SlashCommandBase> _commands;

        private volatile bool _firstReadyRecieved;
        public DiscordCommandsHandler(
            DiscordSocketClient client,
            IContextService contextService,
            TournamentApi api,
            IDataService dataService,
            IStatsApi statsApi,
            IBotApi botApi,
            ITournamentEventsHandler eventsHandler,
            IEventsTimeline timeline,
            IOptions<DiscordBotOptions> options,
            IOptions<TournamentEventsOptions> tournamentOptions)
        {
            _client = client;
            _contextService = contextService;
            _options = options.Value;
            _tournamentOptions = tournamentOptions.Value;

            _commands = new SlashCommandBase[]
            {
                new AddBotsSlashCommand(api),
                new CallSlashCommand(client, dataService, botApi, api),
                new CheckInSlashCommand(dataService, timeline, api),
                new ChekInBotsSlashCommand(api),
                new InfoSlashCommand(api),
                new KickBotsSlashCommand(api),
                new KickPlayerSlashCommand(dataService, eventsHandler, api),
                new LeaveSlashCommand(dataService, eventsHandler, api),
                //new MyIdSlashCommand(),
                new PlayersShashCommand(api),
                new PlaySlashCommand(dataService, statsApi, api, timeline, tournamentOptions),
                new StatusSlashCommand(),
                new TimelineSlashCommand(timeline),
                new TimeSlashCommand(timeline),
                new ViewSlashCommand(api),
                //new StartSlashCommand(timeline, botApi, api, tournamentOptions),
                new DeleteUserDataSlashCommand(dataService),
                new AddTimeSlashCommand(timeline),
                new KickBotSlashCommand(api),
                new SubmitGameSlashCommand(api, timeline),
                new RatedUsersSlashCommand(botApi, dataService),
                new RefreshLeaderboardSlashCommand(botApi, dataService),
                new RefreshLeaderboardV2SlashCommand(botApi, dataService),
                new UserSlashCommand(dataService),
                new AllUsersSlashCommand(botApi, dataService),
                new SetUsersScoreSlashCommand(dataService),
                new RegisterUserSlashCommand(dataService),
                new RebuildCommandsSlashCommand(this),
                new WaitSlashCommand(botApi),
                new BanMapsSlashCommand(dataService, api),
                new DropTournamentSlashCommand(api, timeline, eventsHandler),
                new ForceEventSlashCommand(timeline, eventsHandler),
               // new GoGoGoSlashCommand(botApi),
               // new VoteAddTimeSlashCommand(api),
               // new VoteBanSlashCommand(api),
               // new VoteKickSlashCommand(api),
                new MatchesSlashCommand(api)
            }.ToDictionary(x => x.Name);
        }

        public async Task RebuildCommands()
        {
            foreach (var guild in _client.Guilds)
            {
                await guild.DeleteApplicationCommandsAsync();
                await UpdateOrCreateCommandsForGuild(guild);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _client.Ready += OnClientReady;
            _client.SlashCommandExecuted += OnClientSlashCommandExecuted;

            return Task.CompletedTask;
        }

        private async Task OnClientSlashCommandExecuted(SocketSlashCommand arg)
        {
            var user = arg.User;
            if (user == null || user.IsBot)
                return;

            if (_commands.TryGetValue(arg.CommandName, out var command))
            {
                try
                {
                    var (locale, context) = _contextService.GetLocaleAndContext(arg.Channel.Id);
                    await command.Handle(context, arg, CultureInfo.GetCultureInfo(locale));
                }
                catch (NotImplementedException)
                {
                    await arg.RespondAsync(Text.OfKey(nameof(S.Bot_CommandNotImplemented)).Build(CultureInfo.GetCultureInfo("en")));
                }
            }
            else
            {
                await arg.RespondAsync(Text.OfKey(nameof(S.Bot_UnknownCommand)).Format(arg.CommandName).Build(CultureInfo.GetCultureInfo("en")));
            }
        }

        private async Task OnClientReady()
        {
            if (_firstReadyRecieved)
                return;

            _firstReadyRecieved = true;

            var mainGuild = _client.GetGuild(_options.MainGuildId);

            await UpdateOrCreateCommandsForGuild(mainGuild);

            foreach (var pair in _options.MainThreads)
                await mainGuild.GetTextChannel(pair.Value).SendMessageAsync(Text.OfKey(nameof(S.Bot_Greetings)).Build(CultureInfo.GetCultureInfo(pair.Key)));
        }

        private async Task UpdateOrCreateCommandsForGuild(SocketGuild guild)
        {
            var currentCommands = (await guild.GetApplicationCommandsAsync()).ToList();

            foreach (var cmd in _commands.Values)
            {
                var sameCommand = currentCommands.FirstOrDefault(x => x.Name == cmd.Name);
                if (sameCommand == null)
                {
                    await guild.CreateApplicationCommandAsync(cmd.MakeBuilder().Build());
                }
                else
                {
                    currentCommands.Remove(sameCommand);

                    if (sameCommand.Description != cmd.Description)
                    {
                        await sameCommand.DeleteAsync();
                        await guild.CreateApplicationCommandAsync(cmd.MakeBuilder().Build());
                    }
                }
            }

            foreach (var cmd in currentCommands)
            {
                await cmd.DeleteAsync();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.Ready -= OnClientReady;
            _client.SlashCommandExecuted -= OnClientSlashCommandExecuted;

            return Task.CompletedTask;
        }
    }
}
