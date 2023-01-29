namespace SSTournamentsBot.Api.DataDomain
{
    public class UserInActivityModel
    {
        public ulong DiscordId { get; set; }
        public ulong SteamId { get; set; }
        public int Score { get; set; }
        public int Penalties { get; set; }
    }
}
