using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Raven.Database;
using Raven.Preconditions;

namespace Raven.Modules.Customisations
{
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
            // TODO: Code around the fact that some config files might be made longer than 2000 characters.
            await ReplyAsync(Utils.GetCodeBlock(File.ReadAllText($"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.BaseMenu.ToString()}.txt")));
        }
    }
}
