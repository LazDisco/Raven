using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Database;
using Raven.Services;
using Raven.Utilities;

namespace Raven
{
    public class Startup
    {
        public Startup(string[] args)
        {
            // Load in our data from our JSON config file
            GlobalConfig.LoadConfig(GlobalConfigInstance.GetInstance(
                $@"{AppContext.BaseDirectory}/AppConfig.json"));
            Logger.StartLogger();
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
            }));

            CommandService command = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async,
                ThrowOnError = false
            });

            List<Type> plugins = LoadPlugins();
            List<Task> tasks = new List<Task>();
            foreach (Type plugin in plugins)
                tasks.Add(Task.Run(() => command.AddModuleAsync(plugin, null)));

            Task.WaitAll(tasks.ToArray());

            services.AddSingleton(command)
            .AddSingleton<StartupService>()
            .AddSingleton<DiscordEventHandler>()
            .AddSingleton<Random>();
        }

        private List<Type> LoadPlugins()
        {
            List<Type> pluginList = new List<Type>();
            string dir = Directory.GetCurrentDirectory() + @"\Plugins";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string[] plugins = Directory.EnumerateFiles(dir, "*.dll", SearchOption.TopDirectoryOnly).ToArray();

            int count = 0;
            foreach (string plugin in plugins)
            {
                Assembly asm = null;
                try
                {
                    asm = Assembly.LoadFile(plugin);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Cannot load plugin: {plugin}.", "Startup", LogSeverity.Warning, ex.Message);
                    continue; // Cannot load
                }

                List<Type> modules = new List<Type>();
                Type info = null;

                try
                {
                    Type[] typeList = asm.GetTypes();
                    Assembly core = Assembly.GetExecutingAssembly();
                    Type infoType = core.GetType("Raven.Utilities.PluginInfo");
                    foreach (var t in typeList)
                    {
                        if (t.IsSubclassOf(infoType))
                            info = t;

                        else if (t.BaseType == typeof(ModuleBase<SocketCommandContext>))
                            modules.Add(t);
                    }

                    if (info is null)
                        throw new BadImageFormatException($"{Path.GetFileName(plugin)} is missing plugin information.");

                    if (modules.Count is 0)
                        throw new BadImageFormatException($"{Path.GetFileName(plugin)} is missing plugin modules.");
                        
                    foreach (var module in modules)
                        pluginList.Add(module);

                    GlobalConfig.PluginInfo.Add((PluginInfo)Activator.CreateInstance(info));
                    Logger.Log($"Successfully loaded plugin: {Path.GetFileName(plugin)}. Loaded {modules.Count} modules.", "Startup");
                    count++;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Cannot load plugin: {plugin}.", "Startup", LogSeverity.Warning, ex.Message);
                    continue; // Cannot load;
                }
            }
            Logger.Log($"Successfully loaded {count} plugins.", "Startup", LogSeverity.Verbose);
            return pluginList;
        }
    }
}
