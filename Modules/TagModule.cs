using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Raven.Database;
using Raven.Preconditions;
using static System.String;

namespace Raven.Modules
{
    [RequireContext(ContextType.Guild)]
    [CheckBlacklistedModule]
    [Name("Tag")]
    public class TagModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;
        private readonly DiscordShardedClient _client;

        public TagModule(DiscordShardedClient client, CommandService service)
        {
            _service = service;
            _client = client;
        }

        [CheckBlacklistedCommand]
        [Command("tag list"), Alias("label list")]
        [Summary("Lists all tags, by category")]
        [Priority(1)]
        public async Task TagListAsync()
        {
            var guild = RavenDb.GetGuild(Context.Guild.Id);
            Dictionary<string, List<string>> tags = new Dictionary<string, List<string>>();
            foreach (var tag in guild.Tags)
            {
                if (!tags.ContainsKey(tag.Category))
                    tags.Add(tag.Category, new List<string>());

                tags[tag.Category].Add(tag.Message);
            }

            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = $"{guild.Name}'s Tags";
            foreach (var tag in tags)
                builder.AddField(tag.Key, Join(",\n", tag.Value), true);

            builder.Color = new Color(114, 137, 218);
            await ReplyAsync(null, false, builder.Build());
        }

        [CheckBlacklistedCommand]
        [Command("tag"), Alias("label")]
        [Summary("Fetches guild tag information, quickly reference helpful topics.")]
        [Priority(0)]
        public async Task TagAsync(string name)
        {
            var guild = RavenDb.GetGuild(Context.Guild.Id);
            RavenTag tag = guild.Tags.Find(x => string.Equals(x.Tag, name, StringComparison.CurrentCultureIgnoreCase));
            if (tag is null)
            {
                await ReplyAsync("The specified tag couldn't be found.");
                return;
            }

            await ReplyAsync($"{tag.Tag} ({tag.Category}):\n{tag.Message}");
        }
    }
}
