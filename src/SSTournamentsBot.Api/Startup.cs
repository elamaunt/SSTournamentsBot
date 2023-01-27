using Discord.Commands;
using Discord.WebSocket;
using HttpBuilder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SSTournamentsBot.Api.Services;
using SSTournamentsBot.Services;
using static SSTournaments.SecondaryDomain;
using Microsoft.Extensions.Logging;


namespace SSTournamentsBot.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var config = new DiscordSocketConfig()
            {
                DefaultRetryMode = Discord.RetryMode.AlwaysRetry
            };

            services.Configure<LiteDbOptions>(Configuration.GetSection("LiteDbOptions"));
            services.Configure<DiscordOptions>(Configuration.GetSection("DiscordOptions"));
            services.Configure<TournamentEventsOptions>(Configuration.GetSection("TournamentEventsOptions"));
            services.Configure<DowStatsApiOptions>(Configuration.GetSection("DowStatsApiOptions"));
            services.Configure<DiscordBotOptions>(Configuration.GetSection("DiscordBotOptions"));
            services.Configure<DowStatsGameScannerOptions>(Configuration.GetSection("DowStatsReplayScannerOptions"));

            services.AddControllers();
            services.AddHttpClient()
                .AddSingleton(config)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<HttpService, CustomHttpService>()
                //.AddSingleton<CommandService>()
                .AddSingleton<IEventsTimeline, InMemoryEventsTimeline>()
                .AddTransient<ITournamentEventsHandler, TournamentEventsHandler>()
                .AddSingleton<IGameScanner, DowStatsGameScanner>()
                .AddTransient<IStatsApi, DowStatsApi>()
#if DEBUG
                .AddTransient<IDataService, InMemoryDataService>()
#else
                .AddSingleton<ILiteDbContext, LiteDbContext>()
                .AddSingleton<IDataService, LiteDbDataService>()
#endif

                .AddTransient<IDrawingService, SkiaDrawingService>()
                .AddTransient<IBotApi, DiscordBotApi>()
                .AddTransient<TournamentApi>()
                .AddSingleton<DiscordApi>()
                .AddSingleton<ContextService>()
                
                .AddHostedService<DiscordCommandsHandler>()
                .AddHostedService<DiscordBot>()
                .AddHostedService<TimeSchedulerService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
