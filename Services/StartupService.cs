using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Raven.Services
{
    public class StartupService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordShardedClient _discord;
        private readonly CommandService _commands;

        public StartupService(IServiceProvider provider, DiscordShardedClient discord, CommandService commands)
        {
            _provider = provider;
            _discord = discord;
            _commands = commands;
        }

        public async Task StartAsync()
        {
            if (string.IsNullOrWhiteSpace(GlobalConfig.Token))
                throw new Exception("Please enter your bot's token into the `AppConfig.json` file found in the applications root directory.");

            await _discord.LoginAsync(TokenType.Bot, GlobalConfig.Token);
            await _discord.StartAsync();

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }
    }
}
