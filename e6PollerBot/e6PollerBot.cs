using Discord;
using Discord.Commands;
using Discord.WebSocket;
using e6PollerBot.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace e6PollerBot
{
    class e6PollerBot
    {
        static void Main(string[] args)
            => new e6PollerBot().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args)
        {
            IServiceProvider services = ConfigureServices();
            DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();

            // Register Logging
            client.Log += LogAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;

            // Login Bot.
            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
            await client.StartAsync();

            // Register all other Services.
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            // Sleep the main thread of execution forever.
            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<e6Service>()
                .AddSingleton<DatabaseService>()
                .BuildServiceProvider();
        }
    }
}
