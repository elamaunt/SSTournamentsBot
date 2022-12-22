using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SSTournamentsBot.Api.Services;
using SSTournamentsBot.Services;
using System;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly DiscordApi _api;
        private readonly IDataService _dataService;

        public AuthController(ILogger<AuthController> logger, DiscordApi api, IDataService dataService)
        {
            _logger = logger;
            _api = api;
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

            try
            {
                _dataService.StoreUsersSteamId(result.Value.discordId, result.Value.steamId);

                var html = $"<p>Добро пожаловать на участие в турнирах! Выполните команду play повторно в Discord, чтобы зарегистрироваться.</p>";

                return new ContentResult
                {
                    Content = html,
                    ContentType = "text/html; charset=UTF-8"
                };
            }
            catch (Exception ex)
            {
                return new ContentResult
                {
                    Content = $"<p>{ex.Message}</p>",
                    ContentType = "text/html; charset=UTF-8"
                };
            }
        
        }
    }
}
