using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Raven.Database;
using Raven.Utilities;

namespace Raven.Preconditions
{
    /// <summary>Check if the module is blacklisted in the current guild (doesn't check for guild context, do that first)</summary>
    public class CheckBlacklistedModule : PreconditionAttribute
    {
        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            // Check if this user is a Guild User, which is the only context where roles exist
            if (context.User is SocketGuildUser gUser)
            {
                RavenGuild guild = RavenDb.GetGuild(context.Guild.Id);
                return Task.FromResult(guild.GuildSettings.BlacklistedModules.Any(x => string.Equals(x, command.Module.Name, StringComparison.CurrentCultureIgnoreCase))
                    ? PreconditionResult.FromError("This command is part of a module that has been banned/disallowed in this server.")
                    : PreconditionResult.FromSuccess());
            }

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
