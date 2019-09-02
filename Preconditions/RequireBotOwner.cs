using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Raven.Utilities;

namespace Raven.Preconditions
{
    public class RequireBotOwner : PreconditionAttribute
    {
        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            return Task.FromResult(GlobalConfig.OwnerIds.Any(x => x == context.User.Id)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("You must be the bot owner to do this."));
        }
    }
}
