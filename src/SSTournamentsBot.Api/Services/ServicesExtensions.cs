using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public static class ServicesExtensions
    {
        public static Task Log(this IBotApi api, Context context, string message)
        {
            return api.SendMessage(context, message, GuildThread.Logging);
        }
    }
}
