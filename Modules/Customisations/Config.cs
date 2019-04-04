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
            await ReplyAsync(Utils.GetCodeBlock(
                "# Welcome to the Raven Configuration Menu. Here you can configure almost all settings of the bot.\n" +
                "# Below are the different settings you can configure: \n\n" +

                "1.) Level Settings"
                ));
        }
    }
}
