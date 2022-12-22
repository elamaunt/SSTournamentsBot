using HttpBuilder;
using System.Net.Http;

namespace SSTournamentsBot.Api.Services
{
    public class CustomHttpService : HttpService
    {
        protected override void ConfigureClient(HttpClient client)
        {
            // Nothing
        }

        protected override void ConfigureHandler(HttpClientHandler handler)
        {
            // Nothing
        }
    }
}
