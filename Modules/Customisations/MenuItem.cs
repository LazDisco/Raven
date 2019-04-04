using Discord.Commands;
using Raven.Database;
using System.Threading.Tasks;

namespace Raven.Modules.Customisations
{
    /// <summary>The commands within this module have no permission validation.
    /// Permissions should be handled before setting up the MessageBox values are assigned.</summary>
    public class MenuItem : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;

        public MenuItem(CommandService service)
        {
            _service = service;
        }

        [Command("1")]
        public async Task OneAsync(params string[] args)
        {
            RavenGuild guild = RavenDb.GetGuild(Context.Guild.Id);
            if (!guild.UserConfiguration.ContainsKey(Context.User.Id))
            {
                await ReplyAsync("");
                return;
            }
        }
    }
}
