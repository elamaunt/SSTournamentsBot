using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class KickPlayerSlashCommand : SlashCommandBase
    {
        public override string Name => "kick-player";
        public override string DescriptionKey=> nameof(S.Commands_KickPlayer);

        readonly IDataService _dataService;
        readonly ITournamentEventsHandler _eventsHandler;
        readonly TournamentApi _tournamentApi;
        public KickPlayerSlashCommand(IDataService dataService, ITournamentEventsHandler tournamentEventsHandler, TournamentApi tournamentApi)
        {
            _dataService = dataService;
            _eventsHandler = tournamentEventsHandler;
            _tournamentApi = tournamentApi;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var userOption = arg.Data.Options.First(x => x.Name == "player");
            var user = (IUser)userOption.Value;
            var userData = _dataService.FindUserByDiscordId(user.Id);

            if (userData == null)
            {
                await arg.RespondAsync(OfKey(nameof(S.KickPlayer_NoUser)).Build(culture));
                return;
            }

            var result = await _tournamentApi.TryLeaveUser(userData.DiscordId, userData.SteamId, TechnicalWinReason.OpponentsKicked);

            if (result.IsDone)
            {
                await arg.RespondAsync(OfKey(nameof(S.KickPlayer_PlayerLeftTournament)).Format(user.Username).Build(culture));

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
                await arg.RespondAsync(OfKey(nameof(S.Bot_NoActiveTournament)).Build(culture));
                return;
            }

            if (result.IsNotRegistered)
            {
                await arg.RespondAsync(OfKey(nameof(S.KickPlayer_ImposibleToKickNotRegisteredPlayer)).Build(culture));
                return;
            }

            if (result.IsAlreadyLeftBy)
            {
                var reason = ((LeaveUserResult.AlreadyLeftBy)result).Item;

                if (reason.IsVoting)
                {
                    await arg.RespondAsync(OfKey(nameof(S.KickPlayer_ImposibleToLeaveWhenAlreadyLeftByVoting)).Build(culture));
                    return;
                }

                if (reason.IsOpponentsLeft)
                {
                    await arg.RespondAsync(OfKey(nameof(S.KickPlayer_AlreadyLeftTheTournament)).Build(culture));
                    return;
                }

                if (reason.IsOpponentsBan)
                {
                    await arg.RespondAsync(OfKey(nameof(S.KickPlayer_AlreadyBanned)).Build(culture));
                    return;
                }

                if (reason.IsOpponentsKicked)
                {
                    await arg.RespondAsync(OfKey(nameof(S.KickPlayer_AlreadyKickedByAdmin)).Build(culture));
                    return;
                }
            }

            await arg.RespondAsync(OfKey(nameof(S.KickPlayer_NotSucceded)).Build(culture));
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
