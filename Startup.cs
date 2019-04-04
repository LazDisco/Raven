using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Database;
using Raven.Services;

namespace Raven
{
    public class Startup
    {
        public Startup(string[] args)
        {
            // If in debug mode, copy our JSON config over
            if (Debugger.IsAttached)
                File.Copy($@"{AppContext.BaseDirectory}/../../../AppConfig.json",  $@"{AppContext.BaseDirectory}/AppConfig.json", true);

            // Load in our data from our JSON config file
            GlobalConfig.LoadConfig(GlobalConfigInstance.GetInstance(
                $@"{AppContext.BaseDirectory}/AppConfig.json"));
        }

        public static async Task RunAsync(string[] args)
        {
            var startup = new Startup(args);
            await startup.RunAsync();
        }

        public async Task RunAsync()
        {
            var services = new ServiceCollection();             // Create a new instance of a service collection
            ConfigureServices(services); // Connect all our services
            RavenStore.Initialise(); // Setup our database connection

            var provider = services.BuildServiceProvider();     // Build the service provider
            provider.GetRequiredService<DiscordEventHandler>(); // Connect all our events
            await provider.GetRequiredService<StartupService>().StartAsync();  // Start the startup service
            await Task.Delay(-1);                               // Keep the program alive
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new DiscordShardedClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 40,
                AlwaysDownloadUsers = true,
                TotalShards = GlobalConfig.Shards
            }))
            .AddSingleton(new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async,
                ThrowOnError = false
            }))
            .AddSingleton<StartupService>()
            .AddSingleton<DiscordEventHandler>()
            .AddSingleton<Random>();
        }
    }
}
