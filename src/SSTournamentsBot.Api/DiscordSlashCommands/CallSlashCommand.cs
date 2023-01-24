using Discord;
using Discord.WebSocket;
using Microsoft.FSharp.Core;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class CallSlashCommand : SlashCommandBase
    {
        readonly DiscordSocketClient _client;
        readonly IDataService _dataService;
        readonly IBotApi _botApi;
        readonly TournamentApi _api;

        public CallSlashCommand(DiscordSocketClient client, IDataService dataService, IBotApi botApi, TournamentApi api)
        {
            _client = client;
            _dataService = dataService;
            _botApi = botApi;
            _api = api;
        }

        public override string Name => "call";
        public override string Description => "Позвать оппонента на игру";

        public override async Task Handle(Context context, SocketSlashCommand arg)
        {
            var user = arg.User;

            try
            {
                var match = await _api.FindActiveMatchWith(user.Id);

                if (match == null)
                {
                    await arg.RespondAsync(OfKey(S.Call_YouHaveNotActiveMatches));
                    return;
                }

                async Task Call(Player opponent)
                {
                    var opponentsUser = await _client.GetUserAsync(opponent.DiscordId);

                    if (opponentsUser == null)
                    {
                        await arg.RespondAsync(OfKey(nameof(S.Bot_NoUserInDataBase)));
                        return;
                    }

                    try
                    {
                        await opponentsUser.SendMessageAsync(OfKey(nameof(S.Call_DirectMessageText)).Format(user.Mention));
                    }
                    catch
                    {
                        var opponentData = _dataService.FindUserByDiscordId(opponent.DiscordId);
                        await arg.RespondAsync(OfKey(nameof(S.Call_ImposibleToComply)).Format(opponent.Name));
                        if ((await _api.TryLeaveUser(opponentData.DiscordId, opponentData.SteamId, TechnicalWinReason.OpponentsKicked)).IsDone)
                        {
                            var mention = await _botApi.GetMention(context, opponent.DiscordId);
                            await _botApi.SendMessage(context, OfKey(S.Call_UserKickedFromTournament).Format(mention), GuildThread.EventsTape | GuildThread.TournamentChat);
                        }
                        return;
                    }

                    await arg.RespondAsync(OfKey(S.Call_CalledSuccessfully).Format(opponent.Name));
                }

                if (FSharpOption<Tuple<Player, Race>>.get_IsSome(match.Player1) && match.Player1.Value.Item1.DiscordId != user.Id)
                {
                    await Call(match.Player1.Value.Item1);
                    return;
                }

                if (FSharpOption<Tuple<Player, Race>>.get_IsSome(match.Player2) && match.Player2.Value.Item1.DiscordId != user.Id)
                {
                    await Call(match.Player2.Value.Item1);
                    return;
                }

                await arg.RespondAsync(OfKey(S.Call_NoOpponent));
            }
            catch
            {
                await arg.RespondAsync(OfKey(S.Call_Error));
            }
        }
    }
}
