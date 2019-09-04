﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Raven.Database;
using Raven.Utilities;

namespace Raven.Services.Events
{
    public partial class DiscordEvents
    {
        /// <summary>Called when a user leaves the server (or is kicked). </summary>
        internal async Task GuildUserLeaveAsync(SocketGuildUser user)
        {
            foreach (PluginInfo plugin in GlobalConfig.PluginInfo)
            {
                if (plugin.MessageReceivedAsync != null)
                {
                    if (GlobalConfig.RunPluginFunctionsAsynchronously)
                        #pragma warning disable 4014
                        plugin.GuildUserLeave.Invoke(user);
                        #pragma warning restore 4014
                    else
                        await plugin.GuildUserLeave(user);

                }
            }

            // Get the guild this user is in
            RavenGuild guild = RavenDb.GetGuild(user.Guild.Id) ?? RavenDb.CreateNewGuild(user.Guild.Id, user.Guild.Name);

            // Update the total amount of users
            guild.TotalUsers = (uint)user.Guild.Users.Count;

            // Process goodbye message if one is set
            if (guild.GuildSettings.GoodbyeMessage.Enabled)
            {
                // If the targeted channel is null or no longer exists or the message itself is undefined
                if (guild.GuildSettings.GoodbyeMessage.ChannelId is null || user.Guild.GetTextChannel(guild.GuildSettings.GoodbyeMessage.ChannelId.GetValueOrDefault()) is null
                    || string.IsNullOrWhiteSpace(guild.GuildSettings.GoodbyeMessage.Message))
                {
                    // If the logging channel is setup, exists, and is enabled
                    if (!(guild.LoggingSettings.ChannelId is null) && !(user.Guild.GetTextChannel(guild.LoggingSettings.ChannelId.GetValueOrDefault()) is null)
                        && guild.LoggingSettings.Enabled)
                    {
                        // Log to the logging channel if it has been set
                        await user.Guild.GetTextChannel(guild.LoggingSettings.ChannelId.Value).SendMessageAsync(null, false, new EmbedBuilder()
                        {
                            Title = "Warning!",
                            Color = new Color(255, 128, 0), // Orange
                            Description = "Unable to send goodbye message. Channel or message are currently null. Please reconfigure it.",
                            Footer = new EmbedFooterBuilder()
                            {
                                Text = $"{DateTime.UtcNow:ddd MMM d yyyy HH mm}"
                            }
                        }.Build());
                    }
                }

                else
                {
                    // Send the Goodbye message and repalce the server or user tags if they are present
                    await user.Guild.GetTextChannel(guild.GuildSettings.GoodbyeMessage.ChannelId.Value)
                        .SendMessageAsync(guild.GuildSettings.GoodbyeMessage.Message
                        .Replace("%SERVER%", user.Guild.Name)
                        .Replace("%USER%", user.Username));
                }
            }

            // If the logging channel is setup, exists, and is enabled
            if (!(guild.LoggingSettings.ChannelId is null) && !(user.Guild.GetTextChannel(guild.LoggingSettings.ChannelId.Value) is null)
                                                           && guild.LoggingSettings.Enabled && guild.LoggingSettings.Leave)
            {
                // Log a person has left/been kicked from the server
                string desc = $":package: **{user.Username}#{user.DiscriminatorValue}** has left (or been kicked from) the server.";
                await user.Guild.GetTextChannel(guild.LoggingSettings.ChannelId.Value).SendMessageAsync(null, false, new EmbedBuilder()
                {
                    Description = desc,
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = user.GetAvatarUrl(),
                        Text = $"One Less User ({user.Guild.Users.Count}) | {DateTime.UtcNow:ddd MMM d yyyy HH mm}"
                    },
                    Color = new Color(230, 0, 0)
                }.Build());
            }
        }
    }
}
