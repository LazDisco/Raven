using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Raven.Database;

namespace Raven.Modules.Customisations
{
    public class Prefix : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;

        public Prefix(CommandService service)
        {
            _service = service;
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Command("prefix")]
        public async Task PrefixAsync()
        {
            await ReplyAsync("You did not specify a replacement prefix.");
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Command("prefix")]
        public async Task PrefixAsync(string command)
        {
            RavenGuild guild = RavenDb.GetGuild(Context.Guild.Id);
            guild.GuildSettings.Prefix = command;
            guild.Save();
            await ReplyAsync($"Guild prefix for {guild.Name} has been set to: {command}");
        }
    }
}
