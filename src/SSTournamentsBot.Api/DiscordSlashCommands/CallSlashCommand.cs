﻿using Discord;
using Discord.WebSocket;
using Microsoft.FSharp.Core;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System;
using System.Globalization;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class CallSlashCommand : SlashCommandBase
    {
        readonly DiscordSocketClient _client;
        readonly IDataService _dataService;

        public CallSlashCommand(DiscordSocketClient client, IDataService dataService)
        {
            _client = client;
            _dataService = dataService;
        }

        public override string Name => "call";
        public override string DescriptionKey=> nameof(S.Commands_Call);

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var user = arg.User;

            try
            {
                var match = await context.TournamentApi.FindActiveMatchWith(user.Id);

                if (match == null)
                {
                    await arg.RespondAsync(OfKey(nameof(S.Call_YouHaveNotActiveMatches)).Build(culture));
                    return;
                }

                async Task Call(Player opponent)
                {
                    var opponentsUser = await _client.GetUserAsync(opponent.DiscordId);

                    if (opponentsUser == null)
                    {
                        await arg.RespondAsync(OfKey(nameof(S.Bot_NoUserInDataBase)).Build(culture));
                        return;
                    }

                    try
                    {
                        await opponentsUser.SendMessageAsync(OfKey(nameof(S.Call_DirectMessageText)).Format(user.Mention).Build(culture));
                    }
                    catch
                    {
                        var opponentData = _dataService.FindUserByDiscordId(opponent.DiscordId);
                        await arg.RespondAsync(OfKey(nameof(S.Call_ImposibleToComply)).Format(opponent.Name).Build(culture));
                        if ((await context.TournamentApi.TryLeaveUser(opponentData.DiscordId, opponentData.SteamId, TechnicalWinReason.OpponentsKicked)).IsDone)
                        {
                            var mention = await context.BotApi.GetMention(context, opponent.DiscordId);
                            await context.BotApi.SendMessage(context, OfKey(nameof(S.Call_UserKickedFromTournament)).Format(mention), GuildThread.EventsTape | GuildThread.TournamentChat);
                        }
                        return;
                    }

                    await arg.RespondAsync(OfKey(nameof(S.Call_CalledSuccessfully)).Format(opponent.Name).Build(culture));
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

                await arg.RespondAsync(OfKey(nameof(S.Call_NoOpponent)).Build(culture));
            }
            catch
            {
                await arg.RespondAsync(OfKey(nameof(S.Call_Error)).Build(culture));
            }
        }
    }
}
