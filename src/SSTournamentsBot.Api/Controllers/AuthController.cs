using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        readonly DiscordApi _api;
        readonly IBotApi _botApi;
        readonly IDataService _dataService;

        public AuthController(ILogger<AuthController> logger, DiscordApi api, IBotApi botApi, IDataService dataService)
        {
            _logger = logger;
            _api = api;
            _botApi = botApi;
            _dataService = dataService;
        }

        [HttpGet]
        public async Task<ContentResult> Get(string code)
        {
            var result = await _api.TryGetDiscordIdAndSteamId(code);

            if (!result.HasValue)
            {
                return new ContentResult
                {
                    Content = "<p>Не удалось загрузить информацию о вашем пользователе, либо SteamId не был обнаружен.</p>",
                    ContentType = "text/html; charset=UTF-8"
                };
            }

            if (!_dataService.StoreUsersSteamId(result.Value.discordId, result.Value.steamId))
            {
                return new ContentResult
                {
                    Content = "Такой SteamId уже зарегистрирован на другого пользователя.",
                    ContentType = "text/html; charset=UTF-8"
                };
            }

            var html = $"<h1>SS Tournaments Bot by elamaunt</h1><br/><p>Привязка аккаунта завершена успешно. Добро пожаловать на участие в турнирах! Выполните команду play повторно на турнирном канале в Discord, чтобы зарегистрироваться.</p>";

            await _botApi.SendMessageToUser("Привязка аккаунта завершена успешно. Добро пожаловать на участие в турнирах! Выполните команду __**/play повторно**__ на турнирном канале, чтобы зарегистрироваться.", result.Value.discordId);

            return new ContentResult
            {
                Content = html,
                ContentType = "text/html; charset=UTF-8"
            };
        }
    }
}
