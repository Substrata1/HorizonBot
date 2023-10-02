// Scope: bot
// Permissions: Manage Roles, Ban Members, Read Messages/View Channels, Send Messages, Manage Messages, Read Message History, Use External Emojis, Add Reactions

using DSharpPlus;
using DSharpPlus.CommandsNext;
using HorizonBot.Commands;
using HorizonBot.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HorizonBot
{
    public sealed class Program
    {
        private static Config _config;
        private static Data _data;
        private static DiscordClient _client;
        private static ServiceProvider _services;
        private static CommandsNextExtension _commands;

        static async Task Main(string[] args)
        {
            _config = Config.Load();

            _data = Data.Load();

            _client = new DiscordClient(new DiscordConfiguration
            {
                AutoReconnect = true,
                Intents = DiscordIntents.Guilds | DiscordIntents.GuildMembers | DiscordIntents.GuildMessages | DiscordIntents.GuildMessageReactions | DiscordIntents.MessageContents,
                MinimumLogLevel = LogLevel.Information,
                Token = _config.Token,
                TokenType = TokenType.Bot
            });

            var clientHandler = new ClientHandler(_client, _config, _data);

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_config)
                .AddSingleton(_data)
                .BuildServiceProvider();

            _commands = _client.UseCommandsNext(new CommandsNextConfiguration()
            {
                CaseSensitive = false,
                EnableDefaultHelp = false,
                EnableDms = false,
                EnableMentionPrefix = true,
                Services = _services,
                StringPrefixes = new string[] { _config.Prefix }
            });

            _commands.RegisterCommands<VerificationMessageCommand>();
            _commands.RegisterCommands<RoleSelectCommand>();

            var messageHandler = new MessageHandler(_client, _config);
            var reactionHandler = new ReactionHandler(_client, _config, _data);

            await _client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}