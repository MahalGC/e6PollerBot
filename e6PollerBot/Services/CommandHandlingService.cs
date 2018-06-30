using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace e6PollerBot.Services
{
    public class CommandHandlingService
    {
        private readonly string[] _prefixes;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;
            _prefixes = Environment.GetEnvironmentVariable("PREFIXES").Split(",");

            _client.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            SocketUserMessage message = rawMessage as SocketUserMessage;
            // Ignore message if null or if message is from a Bot.
            if (message is null || message.Author.IsBot) return;

            // Holds the starting position of the command, after the prefix.
            int argPos = 0;

            // If the Prefix is wrong, don't process this message.
            if (!PrefixChecker(message, ref argPos)) return;

            // Get the Context of the message and broadcast the "typing" status until we're done.
            SocketCommandContext context = new SocketCommandContext(_client, message);
            using (IDisposable x = context.Channel.EnterTypingState())
            {
                // Run the command.
                IResult result = await _commands.ExecuteAsync(context, argPos, _services);

                // Log any and all failures.
                if (!result.IsSuccess) Console.WriteLine(result.ErrorReason);

                // Send back Error Message if there is one, but do not send "Unknown Command" Error Messages.
                if (result.Error.HasValue && result.Error.Value != CommandError.UnknownCommand)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
        }

        private bool PrefixChecker(SocketUserMessage message, ref int argPos)
        {
            foreach (string prefix in _prefixes)
            {
                if (message.HasStringPrefix(prefix, ref argPos)) return true;
            }
            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos)) return true;

            return false;
        }
    }
}
