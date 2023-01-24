using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class KickPlayerSlashCommand : SlashCommandBase
    {
        public override string Name => "kick-player";
        public override string Description => "Исключить игрока из турнира";

        readonly IDataService _dataService;
        readonly ITournamentEventsHandler _eventsHandler;
        readonly TournamentApi _tournamentApi;
        public KickPlayerSlashCommand(IDataService dataService, ITournamentEventsHandler tournamentEventsHandler, TournamentApi tournamentApi)
        {
            _dataService = dataService;
            _eventsHandler = tournamentEventsHandler;
            _tournamentApi = tournamentApi;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg)
        {
            var userOption = arg.Data.Options.First(x => x.Name == "player");
            var user = (IUser)userOption.Value;
            var userData = _dataService.FindUserByDiscordId(user.Id);

            if (userData == null)
            {
                await arg.RespondAsync(OfKey(nameof(S.KickPlayer_NoUser)));
                return;
            }

            var result = await _tournamentApi.TryLeaveUser(userData.DiscordId, userData.SteamId, TechnicalWinReason.OpponentsKicked);

            if (result.IsDone)
            {
                await arg.RespondAsync(OfKey(nameof(S.KickPlayer_PlayerLeftTournament)).Format(user.Username));

                if (_tournamentApi.IsTournamentStarted)
                {
                    if (_tournamentApi.ActiveMatches.All(x => !x.Result.IsNotCompleted))
                        await _eventsHandler.DoCompleteStage(context.Name);
                }
                else
                {
                    if (_tournamentApi.IsCheckinStage && _tournamentApi.IsAllPlayersCheckIned)
                        await _eventsHandler.DoStartCurrentTournament(context.Name);
                }
                return;
            }

            if (result.IsNoTournament)
            {
                await arg.RespondAsync(OfKey(nameof(S.Bot_NoActiveTournament)));
                return;
            }

            if (result.IsNotRegistered)
            {
                await arg.RespondAsync(OfKey(nameof(S.KickPlayer_ImposibleToKickNotRegisteredPlayer)));
                return;
            }

            if (result.IsAlreadyLeftBy)
            {
                var reason = ((LeaveUserResult.AlreadyLeftBy)result).Item;

                if (reason.IsVoting)
                {
                    await arg.RespondAsync(OfKey(nameof(S.KickPlayer_ImposibleToLeaveWhenAlreadyLeftByVoting)));
                    return;
                }

                if (reason.IsOpponentsLeft)
                {
                    await arg.RespondAsync(OfKey(nameof(S.KickPlayer_AlreadyLeftTheTournament)));
                    return;
                }

                if (reason.IsOpponentsBan)
                {
                    await arg.RespondAsync(OfKey(nameof(S.KickPlayer_AlreadyBanned)));
                    return;
                }

                if (reason.IsOpponentsKicked)
                {
                    await arg.RespondAsync(OfKey(nameof(S.KickPlayer_AlreadyKickedByAdmin)));
                    return;
                }
            }

            await arg.RespondAsync(OfKey(nameof(S.KickPlayer_NotSucceded)));
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("player")
                    .WithDescription("Игрок для исключения")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.User))
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
