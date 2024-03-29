﻿using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class BanMapsSlashCommand : SlashCommandBase
    {
        public override string Name => "ban-maps";
        public override string DescriptionKey=> nameof(S.Commands_BanMaps);

        readonly IDataService _dataService;

        public BanMapsSlashCommand(IDataService dataService)
        {
            _dataService = dataService;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var userData = _dataService.FindUserByDiscordId(arg.User.Id);

            if (userData == null)
            {
                await arg.RespondAsync(OfKey(nameof(S.Bot_YouAreNotRegistered)).Build(culture));
                return;
            }

            await arg.DeferAsync();

            Task Responce(string message) => arg.ModifyOriginalResponseAsync(x => x.Content = message);

            userData.Map1v1Bans = GetMapBans(arg);

            if (!_dataService.UpdateUser(userData))
            {
                await Responce(OfKey(nameof(S.Bot_DataBaseUpdateError)).Build(culture));
                return;
            }

            if (context.TournamentApi.IsTournamentStarted)
            {
                await Responce(OfKey(nameof(S.BanMaps_MapsUpdatedButForNextTournaments)).Build(culture));
            }
            else
            {
                var result = await context.TournamentApi.TryUpdatePlayer(userData);

                if (result.IsCompleted || result.IsNoTournament || result.IsNotRegistered)
                {
                    await Responce(OfKey(nameof(S.BanMaps_UpdatedSuccessfully)).Build(culture));
                    return;
                }

                await Responce(OfKey(nameof(S.BanMaps_MapsUpdatedButForNextTournaments)).Build(culture));
            }
        }

        private MapBans GetMapBans(SocketSlashCommand arg)
        {
            var bans = MapBans.None;

            bans |= GetOptionValue(arg, "ban-1");
            bans |= GetOptionValue(arg, "ban-2");
            bans |= GetOptionValue(arg, "ban-3");
            bans |= GetOptionValue(arg, "ban-4");
            bans |= GetOptionValue(arg, "ban-5");

            return bans;
        }

        private MapBans GetOptionValue(SocketSlashCommand arg, string name)
        {
            var banOption = arg.Data.Options.FirstOrDefault(x => x.Name == name);

            if (banOption == null)
                return MapBans.None;

            switch ((long)banOption.Value)
            {
                case 0: return MapBans.None;
                case 1: return MapBans.NoBloodRiver;
                case 2: return MapBans.NoFataMorgana;
                case 3: return MapBans.NoFallenCity;
                case 4: return MapBans.NoMeetingOfMinds;
                case 5: return MapBans.NoDeadlyFunArcheology;
                case 6: return MapBans.NoSugarOasis;
                case 7: return MapBans.NoOuterReaches;
                case 8: return MapBans.NoBattleMarshes;
                case 9: return MapBans.NoShrineOfExcellion;
                case 10: return MapBans.NoTranquilitysEnd;
                case 11: return MapBans.NoTitansFall;
                case 12: return MapBans.NoQuestsTriumph;
                default: return MapBans.None;
            }
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            AddBanOption(builder, "ban-1", "Первый бан");
            AddBanOption(builder, "ban-2", "Второй бан");
            AddBanOption(builder, "ban-3", "Третий бан");
            AddBanOption(builder, "ban-4", "Четвертый бан");
            AddBanOption(builder, "ban-5", "Пятый бан");
        }

        private void AddBanOption(SlashCommandBuilder builder, string name, string description)
        {
            builder.AddOption(new SlashCommandOptionBuilder()
                .WithName(name)
                .WithRequired(false)
                .AddChoice("---", 0)
                .AddChoice("Blood River", 1)
                .AddChoice("Fata Morgana", 2)
                .AddChoice("Fallen City", 3)
                .AddChoice("Meeting Of Minds", 4)
                .AddChoice("Deadly Fun Archeology", 5)
                .AddChoice("Sugar Oasis", 6)
                .AddChoice("Outer Reaches", 7)
                .AddChoice("Battle Marshes", 8)
                .AddChoice("Shrine Of Excellion", 9)
                .AddChoice("Tranquility's End", 10)
                .AddChoice("Titan's Fall", 11)
                .AddChoice("Quest's Triumph", 12)
                .WithType(ApplicationCommandOptionType.Integer)
                .WithDescription(description));
        }
    }
}
