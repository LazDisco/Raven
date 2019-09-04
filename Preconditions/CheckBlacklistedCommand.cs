using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Raven.Database;

namespace Raven.Preconditions
{
    /// <summary>Check if the command is blacklisted in the current guild</summary>
    public class CheckBlacklistedCommand : PreconditionAttribute
    {
        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            // Check if this user is a Guild User, which is the only context where roles exist
            if (context.User is SocketGuildUser gUser)
            {
                RavenGuild guild = RavenDb.GetGuild(context.Guild.Id);
                return Task.FromResult(
                    guild.GuildSettings.BlacklistedCommands.Any(x => string.Equals(x, command.Name, StringComparison.CurrentCultureIgnoreCase)) ||
                    guild.GuildSettings.BlacklistedCommands.Intersect(command.Aliases).Any()
                        ? PreconditionResult.FromError("This command has been banned/disallowed in this server.")
                        : PreconditionResult.FromSuccess());
            }

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
