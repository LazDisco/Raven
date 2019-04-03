using Discord.Commands;
using Raven.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Raven.Modules.Customisations
{
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
            
            guild.Save();
            await ReplyAsync($"Guild prefix for {guild.Name} has been set to: {command}");
        }
    }
}
