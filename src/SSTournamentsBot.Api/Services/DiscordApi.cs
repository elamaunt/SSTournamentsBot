using HttpBuilder;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SSTournamentsBot.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SSTournamentsBot.Services
{
    public class DiscordApi
    {
        readonly HttpService _service;
        readonly DiscordOptions _options;

        public DiscordApi(HttpService service, IOptions<DiscordOptions> options)
        {
            _service = service;
            _options = options.Value;
        }

        internal async Task<(ulong discordId, ulong steamId)?> TryGetDiscordIdAndSteamId(string code)
        {
            try
            {
                var tokenResponse = await PostAndParse<DiscordToken, FormUrlEncodedContent>($"{_options.ApiEndPoint}/oauth2/token", new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("client_id", _options.ClientId),
                    new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("grant_type","authorization_code"),
                    new KeyValuePair<string, string>("redirect_uri", _options.AuthEndpoint)
                }));

                var token = tokenResponse.access_token;

                var user = await GetAndParse<DiscordUser>($"{_options.ApiEndPoint}/users/@me", token);

                if (user == null)
                    return null;

                if (!ulong.TryParse(user.id, out var discordId))
                    return null;

                var connections = await GetAndParse<DiscordConnection[]>($"{_options.ApiEndPoint}/users/@me/connections", token);

                if (connections == null)
                    return null;

                var steamConnection = connections.FirstOrDefault(x => x.type == "steam");

                if (steamConnection == null || !steamConnection.verified || steamConnection.revoked)
                    return null;

                if (!ulong.TryParse(steamConnection.id, out var steamId))
                    return null;

                return (discordId, steamId);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        private Task<T> PostAndParse<T, B>(string url, B content) where B : HttpContent
        {
            return _service.Build(url, UriKind.Absolute)
                .WithContent(content)
                .Post()
                .Send()
                .ValidateSuccessStatusCode()
                .Json<T>(JsonSerializer.CreateDefault()).Task;
        }
        private Task<T> GetAndParse<T>(string url, string bearer)
        {
            return _service.Build(url, UriKind.Absolute)
                .AddHeaderIfNotAdded("Authorization", $"Bearer {bearer}")
                .Get()
                .Send()
                .ValidateSuccessStatusCode()
                .Json<T>(JsonSerializer.CreateDefault()).Task;
        }

        public class DiscordUser
        {
            public string id { get; set; }
            public string username { get; set; }
            public string discriminator { get; set; }
            public string avatar { get; set; }
            public bool verified { get; set; }
            public string email { get; set; }
            public int? flags { get; set; }
            public string banner { get; set; }
            public int? accent_color { get; set; }
            public int? premium_type { get; set; }
            public int? public_flags { get; set; }
        }

        public class DiscordConnection
        {
            public string id { get; set; } // id of the connection account
            public string name { get; set; } // the username of the connection account
            public string type { get; set; } // the service of this connection
            public bool revoked { get; set; } // whether the connection is revoked
            public object[] integrations { get; set; } // an array of partial server integrations
            public bool verified { get; set; } // whether the connection is verified
            public bool friend_sync { get; set; } // whether friend sync is enabled for this connection
            public bool show_activity { get; set; } // whether activities related to this connection will be shown in presence updates
            public bool two_way_link { get; set; } // whether this connection has a corresponding third party OAuth2 token
            public int visibility { get; set; }
        }

        public class DiscordToken
        {
            public string access_token { get; set; }
            public ulong expires_in { get; set; }
            public string refresh_token { get; set; }
            public string scope { get; set; }
            public string token_type { get; set; }
        }

        public class DiscordTokenRequest
        {
            public string client_id { get; set; }//"YOUR APP ID";
            public string client_secret { get; set; } //"YOUR CLIENT SECRET";
            public string grant_type { get; set; } //"authorization_code";
            public string code { get; set; } //"CODE FROM USER ACCEPTING INVITE";
            public string redirect_uri { get; set; } //"http://localhost/discord/redirect";
        }

    }
}
