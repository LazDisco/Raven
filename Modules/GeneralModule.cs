using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using Discord.WebSocket;
using Raven;
using Raven.Preconditions;
using Raven.Utilities;

namespace Raven.Modules
{
    [CheckBlacklistedModule]
    [Name("General")]
    public class GeneralModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;
        private readonly DiscordShardedClient _client;

        public GeneralModule(DiscordShardedClient client, CommandService service)
        {
            _service = service;
            _client = client;
        }

        [CheckBlacklistedCommand]
        [Command("version"), Alias("v")]
        [Summary("Fetches the version information for those curious.")]
        public async Task VersionAsync() => await ReplyAsync($"RavenBot is currently running version: {Assembly.GetExecutingAssembly().GetName().Version}");

        [CheckBlacklistedCommand]
        [Command("about")]
        [Summary("Fetches information on the bot/creator.")]
        public async Task AboutAsync()
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "Raven was made with love (and a little boredom) by Laz#9427.",
                Title = "About Raven:",
                Footer = new EmbedFooterBuilder{Text = "Got ideas for more features, or maybe have noticed some bugs? Welcome questions, requests, issues, and all.\n" +
                                                        "[Post Here](https://github.com/LazDisco/Raven/issues)"}

            };
            builder.AddField("Version", Assembly.GetExecutingAssembly().GetName().Version, false);
            await ReplyAsync("", false, builder.Build());
        }
    }
}
