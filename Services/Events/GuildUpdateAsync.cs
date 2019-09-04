using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Raven.Database;
using Raven.Utilities;

namespace Raven.Services.Events
{
    public partial class DiscordEvents
    {
        /// <summary>Called when a change is made to a server. </summary>
        internal async Task GuildUpdateAsync(SocketGuild oldGuild, SocketGuild newGuild)
        {
            foreach (PluginInfo plugin in GlobalConfig.PluginInfo)
            {
                if (plugin.MessageReceivedAsync != null)
                {
                    if (GlobalConfig.RunPluginFunctionsAsynchronously)
                        #pragma warning disable 4014
                        plugin.GuildUpdate(oldGuild, newGuild);
                        #pragma warning restore 4014
                    else
                        await plugin.GuildUpdate(oldGuild, newGuild);
                }
            }

            RavenGuild guild = RavenDb.GetGuild(oldGuild.Id);

            // Compare changes
            if (oldGuild.Name != newGuild.Name)
                guild.Name = newGuild.Name;

            guild.TotalUsers = (uint)newGuild.Users.Count;
            guild.Save();
        }
    }
}
