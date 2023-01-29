using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using SSTournamentsBot.Services;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        readonly ILogger<AuthController> _logger;
        readonly IContextService _contextService;
        readonly DiscordApi _api;
        readonly IBotApi _botApi;
        readonly IDataService _dataService;

        public AuthController(ILogger<AuthController> logger, IContextService contextService, DiscordApi api, IBotApi botApi, IDataService dataService)
        {
            _logger = logger;
            _contextService = contextService;
            _api = api;
            _botApi = botApi;
            _dataService = dataService;
        }

        [HttpGet]
        public async Task<ContentResult> Get(string code)
        {
            var result = await _api.TryGetDiscordIdAndSteamId(code);
            var culture = System.Globalization.CultureInfo.GetCultureInfo("en");

            if (!result.HasValue)
            {
                return new ContentResult
                {
                    Content = Text.OfKey(nameof(S.Bot_SteamIdNotFoundHtml)).Build(culture),
                    ContentType = "text/html; charset=UTF-8"
                };
            }

            if (!_dataService.StoreUsersSteamId(result.Value.discordId, result.Value.steamId))
            {
                return new ContentResult
                {
                    Content = Text.OfKey(nameof(S.Bot_SteamIdAlreadyUsedHtml)).Build(culture),
                    ContentType = "text/html; charset=UTF-8"
                };
            }

            var html = Text.OfKey(nameof(S.Bot_AccountRegisteredSuccessfullyHtml)).Build(culture);

            await _botApi.SendMessageToUser(_contextService.GetMainContext(), Text.OfKey(nameof(S.Bot_AccountRegisteredSuccessfully)), result.Value.discordId);

            return new ContentResult
            {
                Content = html,
                ContentType = "text/html; charset=UTF-8"
            };
        }
    }
}
