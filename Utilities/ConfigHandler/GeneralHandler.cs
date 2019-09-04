using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Rest;
using Discord.WebSocket;
using Raven.Database;

namespace Raven.Utilities.ConfigHandler
{
    public static partial class ConfigHandler
    {
        private static Task<RestUserMessage> GeneralSetPrefix(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null) // If they didn't provide a channel name
                return channel.SendMessageAsync(GetMissingParam("Prefix", typeof(string)));

            guild.GuildSettings.Prefix = string.Join(' ', args.Skip(1));
            guild.UserConfiguration[userId] = MessageBox.GeneralSettings;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.GeneralSettings);
        }
    }
}
