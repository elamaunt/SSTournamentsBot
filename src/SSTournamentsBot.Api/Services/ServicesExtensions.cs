using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public static class ServicesExtensions
    {
        public static Task Log(this IBotApi api, string message)
        {
            return api.SendMessage(message, GuildThread.Logging);
        }
    }
}
