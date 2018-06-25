using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace e6PollerBot
{
    class Program
    {
        static void Main(string[] args) => new Program().RunBotAsync(args).GetAwaiter().GetResult();

        private static string _botToken;
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        public async Task RunBotAsync(string[] args)
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            _botToken = args[0];
        }
    }
}
