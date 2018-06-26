using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace e6PollerBot
{
    class e6PollerBot
    {
        static void Main(string[] args) => new e6PollerBot().RunBotAsync(args).GetAwaiter().GetResult();

        public static readonly string[] _prefixes = { "!pb ", ".pb ", "$pb ", "&pb ", "!pb", ".pb", "$pb", "&pb"};

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

            // Event Subscriptions
            _client.Log += Log;

            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, _botToken);

            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);

            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private bool PrefixChecker(SocketUserMessage message, ref int argPos)
        {
            foreach (string prefix in _prefixes)
            {
                if (message.HasStringPrefix(prefix, ref argPos))
                {
                    return true;
                }
            }

            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                return true;
            }

            return false;
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            SocketUserMessage message = arg as SocketUserMessage;

            if (message is null || message.Author.IsBot)
            {
                return;
            }

            int argPos = 0;

            if (PrefixChecker(message, ref argPos))
            {
                SocketCommandContext context = new SocketCommandContext(_client, message);

                IResult result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess)
                {
                    Console.WriteLine(result.ErrorReason);
                }
            }
        }
    }
}
