using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Raven.Database;
using Raven.Preconditions;
using Raven.Utilities;
using Raven.Utilities.ConfigHandler;

namespace Raven.Modules
{
    [RequireContext(ContextType.Guild)]
    [Name("Config")]
    public class ConfigModule : ModuleBase<ShardedCommandContext>
    {
        private readonly CommandService _service;

        public ConfigModule(CommandService service)
        {
            _service = service;
        }

        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireBotOwner(Group = "Permission")]
        [Command("config")]
        public async Task ConfigAsync()
        {
            RavenGuild guild = RavenDb.GetGuild(Context.Guild.Id);
            guild.UserConfiguration[Context.User.Id] = MessageBox.BaseMenu;
            guild.Save();
            await ReplyAsync(ConfigHandler.GetCodeBlock(File.ReadAllText($"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.BaseMenu.ToString()}.txt")));
        }

        [Command("about")]
        public async Task AboutAsync()
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder() { Name = "Laz" },
                Color = new Color(114, 137, 218),
                Description = "Raven was made with Love by Laz#9427.",
                Title = "About Raven:"
            };
            await ReplyAsync("", false, builder.Build());
        }
    }
}
