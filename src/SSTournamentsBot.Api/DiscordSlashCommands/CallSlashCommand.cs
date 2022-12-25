using Discord;
using Discord.WebSocket;
using Microsoft.FSharp.Core;
using SSTournamentsBot.Api.Services;
using System;
using System.Threading.Tasks;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class CallSlashCommand : SlashCommandBase
    {
        readonly DiscordSocketClient _client;
        readonly TournamentApi _api;

        public CallSlashCommand(DiscordSocketClient client, TournamentApi api)
        {
            _client = client;
            _api = api;
        }

        public override string Name => "call";
        public override string Description => "Позвать оппонента на игру";

        public override async Task Handle(SocketSlashCommand arg)
        {
            var user = arg.User;

            try
            {
                var match = _api.FindActiveMatchWith(user.Id);

                if (match == null)
                {
                    await arg.RespondAsync($"У вас нет активных матчей в данный момент.");
                    return;
                }

                async Task Call(Player opponent)
                {
                    var opponentsUser = await _client.GetUserAsync(opponent.DiscordId);

                    if (opponentsUser == null)
                    {
                        await arg.RespondAsync($"Такого пользователя не существует.");
                        return;
                    }

                    try
                    {
                        await opponentsUser.SendMessageAsync($"Игрок под ником {user.Mention} призывает Вас начать матч. Пожалуйста, свяжитесь с ним в чат канале турниров.");
                    }
                    catch
                    {
                        // TODO: kick the user
                        await arg.RespondAsync($"Ваш оппонент под ником {opponent.Name} не может быть вызван. Возможно, он покинул сервер. \nВо всех матчах ему будет присуждено техническое поражение.");
                        return;
                    }


                    await arg.RespondAsync($"Ваш оппонент под ником {opponent.Name} вызван.");
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

                await arg.RespondAsync($"В данный момент ваш оппонент еще не определен.");
            }
            catch
            {
                await arg.RespondAsync($"Возникла ошибка при вызове игрока");
            }
        }
    }
}
