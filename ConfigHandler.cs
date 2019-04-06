﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Rest;
using Discord.WebSocket;
using Raven.Database;

namespace Raven
{
    public static class ConfigHandler
    {
        /// <summary>Returns a string that is formatted in a HSP code block. Used for config menus mostly.</summary>
        public static string GetCodeBlock(string contents)
        {
            if (contents.Length > 1900)
                return "Unable to create config menu. Config menu too close to 2000 character limit cap. Please remove some words.";
            return "```hsp\n" + contents + "\n\n# Specify an option by typing the number next to it.\n" +
                   "# You can return to the previous menu by typing 'back'.\n" +
                   "# You can exit the menu by typing 'exit'.\n" + 
                   "# Regular commands will not work while in the menu.```";
        }

        /// <summary>Convert a word that is formatted in pascal case to have splits (by space) at each upper case letter.</summary>
        public static string SplitPascalCase(string convert)
        {
            return Regex.Replace(Regex.Replace(convert, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
        }

        private static string GetMissingParam(string missing, Type type)
        {
            return $"You didn't provide a required argument. MissingParam: {missing} ({type.Name})";
        }

        private static string ParamWrongFormat(string param, Type type)
        {
            return $"One of the arguments you provided was in the wrong format. Param: {param} (Expected type: {type.Name})";
        }

        private static Task<RestUserMessage> InvalidOption(ISocketMessageChannel channel)
        {
            return channel.SendMessageAsync("The option you specified doesn't exist. The option should be"
                                            + " just the number of the option you are trying to pick. Try again.");
        }

        /// <summary>Select a new menu option to jump to.</summary>
        public static Task<RestUserMessage> SelectSubMenu(RavenGuild guild, ulong userId, SocketTextChannel channel,
            MessageBox option)
        {
            switch (option)
            {
                case MessageBox.LevelSettings:
                    guild.UserConfiguration[userId] = MessageBox.LevelSettings;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(
                        File.ReadAllText($@"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.LevelSettings}.txt"))
                        .Replace("%MinXP%", guild.GuildSettings.LevelConfig.MinXpGenerated.ToString())
                        .Replace("%MaxXP%", guild.GuildSettings.LevelConfig.MaxXpGenerated.ToString())
                        .Replace("%XPTime%", guild.GuildSettings.LevelConfig.SecondsBetweenXpGiven.ToString()));

                case MessageBox.WelcomeSettings:
                    guild.UserConfiguration[userId] = MessageBox.WelcomeSettings;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(
                            File.ReadAllText(
                                $@"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.WelcomeSettings}.txt"))
                        .Replace("%state%", guild.GuildSettings.WelcomeMessage.Enabed ? "Enabled" : "Disabled")
                        .Replace("%channel%", guild.GuildSettings.WelcomeMessage.ChannelId == null
                            ? "Not Set"
                            : channel.Guild.GetTextChannel(guild.GuildSettings.WelcomeMessage.ChannelId.Value) == null
                                ? "DELETED CHANNEL"
                                : "#" + channel.Guild.GetTextChannel(guild.GuildSettings.WelcomeMessage.ChannelId.Value).Name));

                case MessageBox.GoodbyeSettings:
                    guild.UserConfiguration[userId] = MessageBox.GoodbyeSettings;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(
                            File.ReadAllText(
                                $@"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.GoodbyeSettings}.txt"))
                        .Replace("%state%", guild.GuildSettings.GoodbyeMessage.Enabed ? "Enabled" : "Disabled")
                        .Replace("%channel%", guild.GuildSettings.GoodbyeMessage.ChannelId == null
                            ? "Not Set"
                            : channel.Guild.GetTextChannel(guild.GuildSettings.GoodbyeMessage.ChannelId.Value) == null
                                ? "DELETED CHANNEL"
                                : "#" + channel.Guild.GetTextChannel(guild.GuildSettings.GoodbyeMessage.ChannelId.Value).Name));

                case MessageBox.LsSettingSubmenu:
                    guild.UserConfiguration[userId] = MessageBox.LsSettingSubmenu;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(
                        File.ReadAllText($@"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.LsSubSettings}.txt"))
                        .Replace("%CurrentSetting%", SplitPascalCase(guild.GuildSettings.LevelConfig.LevelSettings.ToString())));

                case MessageBox.BaseMenu:
                    guild.UserConfiguration[userId] = MessageBox.BaseMenu;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(File.ReadAllText(
                        $"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.BaseMenu.ToString()}.txt")));
                default:
                    guild.UserConfiguration.Remove(userId);
                    guild.Save();
                    return channel.SendMessageAsync("I don't know how you got here, but I'm going to termiante this menu just in case.");
            }
        }

        /// <summary>Process the actual options</summary>
        public static Task<RestUserMessage> SelectOption(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (!int.TryParse(args[0], out int temp))
                return InvalidOption(channel);

            MessageBox option = (MessageBox) temp;

            switch (guild.UserConfiguration[userId])
            {
                // Don't know how this would be possible, but better safe than sorry.
                case MessageBox.BaseMenu:
                    return SelectSubMenu(guild, userId, channel, option);

                // Level Settings Sub Menu
                case MessageBox.LevelSettings:
                {
                    if ((int) option is 4
                    ) // We need to cast our submenus to a higher value otherwise they cause problems when selecting options
                        option = MessageBox.LsSettingSubmenu;
                    switch (option)
                    {
                        case MessageBox.LsSetMinXp:
                            return LsSetMinXp(guild, userId, channel, args);

                        case MessageBox.LsSetMaxXp:
                            return LsSetMaxXp(guild, userId, channel, args);

                        case MessageBox.LsSetXpTime:
                            return LsSetMinXpTime(guild, userId, channel, args);

                        case MessageBox.LsSettingSubmenu:
                            return SelectSubMenu(guild, userId, channel, option);

                        default:
                            return InvalidOption(channel);
                    }
                }
                
                // Level Settings Sub-Sub Menu
                case MessageBox.LsSettingSubmenu:
                {
                    switch (option)
                    {
                        case MessageBox.LsSettingDisabled:
                            return LsSetLevelState(guild, userId, channel, LevelSettings.Disabled);

                        case MessageBox.LsSettingGlobalLevel:
                            return LsSetLevelState(guild, userId, channel, LevelSettings.GlobalLeveling);

                        case MessageBox.LsSettingGuildLevel:
                            return LsSetLevelState(guild, userId, channel, LevelSettings.GuildLeveling);

                        default:
                            return InvalidOption(channel);
                    }
                }

                // Welcome Message Settings Sub Menu
                case MessageBox.WelcomeSettings:
                {
                    switch (option)
                    {
                        case MessageBox.WelcomeToggle:
                            guild.GuildSettings.WelcomeMessage.Enabed = !guild.GuildSettings.WelcomeMessage.Enabed;
                            guild.UserConfiguration[userId] = MessageBox.WelcomeSettings;
                            guild.Save();
                            return SelectSubMenu(guild, userId, channel, MessageBox.WelcomeSettings);

                        case MessageBox.WelcomeChannel:
                            return WelcomeSetChannel(guild, userId, channel, args);

                        case MessageBox.WelcomeMessage:
                            return WelcomeSetMessage(guild, userId, channel, args);

                        case MessageBox.WelcomePreview:
                            return WelcomePreviewMessage(guild, userId, channel, args);

                        default:
                            return InvalidOption(channel);
                    }
                }

                case MessageBox.GoodbyeSettings:
                {
                    switch (option)
                    {
                        case MessageBox.GoodbyeToggle:
                            guild.GuildSettings.GoodbyeMessage.Enabed = !guild.GuildSettings.GoodbyeMessage.Enabed;
                            guild.UserConfiguration[userId] = MessageBox.GoodbyeSettings;
                            guild.Save();
                            return SelectSubMenu(guild, userId, channel, MessageBox.GoodbyeSettings);

                        case MessageBox.GoodbyeChannel:
                            return GoodbyeSetChannel(guild, userId, channel, args);

                        case MessageBox.GoodbyeMessage:
                            return GoodbyeSetMessage(guild, userId, channel, args);

                        case MessageBox.GoodbyePreview:
                            return GoodbyePreviewMessage(guild, userId, channel, args);

                        default:
                            return InvalidOption(channel);
                    }
                }

                default:
                    guild.UserConfiguration.Remove(userId);
                    guild.Save();
                    return channel.SendMessageAsync("I don't know how you got here, but I am gonna exit the menu just to be safe.\n" +
                                                    "(You should probably report this if it's reproducible.)");
            }
        }

        //private static Task<RestUserMessage> TEMPLATEFUNCTION(RavenGuild guild, ulong userId, ISocketMessageChannel channel, string[] args) {}

        private static Task<RestUserMessage> LsSetLevelState(RavenGuild guild, ulong userId, SocketTextChannel channel, LevelSettings option)
        {
            guild.GuildSettings.LevelConfig.LevelSettings = option;
            guild.UserConfiguration[userId] = MessageBox.LsSettingSubmenu;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.LsSettingSubmenu);
        }

        private static Task<RestUserMessage> LsSetMinXp(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("MinimumXp", typeof(int)));
            if (!int.TryParse(args.ElementAt(1), out int val))
                return channel.SendMessageAsync(ParamWrongFormat("MinimumXp", typeof(int)));

            // TODO: Unhardcode clamped global xp values
            if (val <= 0)
                return channel.SendMessageAsync("MinimumXp must be greater than 0.");
            if (val > 999)
                return channel.SendMessageAsync("MinimumXp must not be greater than 999.");
            if (val > guild.GuildSettings.LevelConfig.MaxXpGenerated)
                return channel.SendMessageAsync("MinimumXp must not be less than the MaximumXp. They can be equal to remove RNG.");

            guild.GuildSettings.LevelConfig.MinXpGenerated = val;
            guild.UserConfiguration[userId] = MessageBox.LevelSettings;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.LevelSettings);
        }

        private static Task<RestUserMessage> LsSetMaxXp(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("MaximumXp", typeof(int)));
            if (!int.TryParse(args.ElementAt(1), out int val))
                return channel.SendMessageAsync(ParamWrongFormat("MaximumXp", typeof(int)));

            // TODO: Unhardcode clamped global xp values
            if (val <= 1)
                return channel.SendMessageAsync("MaximumXp must be greater than 1.");
            if (val > 1000)
                return channel.SendMessageAsync("MaximumXp must not be greater than 1000.");
            if (val < guild.GuildSettings.LevelConfig.MinXpGenerated)
                return channel.SendMessageAsync("MaximumXp must not be less than the MinimumXp. They can be equal to remove RNG.");

            guild.GuildSettings.LevelConfig.MaxXpGenerated = val;
            guild.UserConfiguration[userId] = MessageBox.LevelSettings;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.LevelSettings);
        }

        private static Task<RestUserMessage> LsSetMinXpTime(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("MinXpTime", typeof(int)));
            if (!int.TryParse(args.ElementAt(1), out int val))
                return channel.SendMessageAsync(ParamWrongFormat("MinXpTime", typeof(int)));

            // TODO: Unhardcode clamped global xp values
            if (val < 30)
                return channel.SendMessageAsync("MinXpTime must be greater than or equal to 30.");
            if (val > 180)
                return channel.SendMessageAsync("MinXpTime must not be greater than 180.");

            guild.GuildSettings.LevelConfig.SecondsBetweenXpGiven = (uint)val;
            guild.UserConfiguration[userId] = MessageBox.LevelSettings;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.LevelSettings);
        }

        private static Task<RestUserMessage> WelcomeSetChannel(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null) // If they didn't provide a channel name
                return channel.SendMessageAsync(GetMissingParam("ChannelName", typeof(string)));

            Console.WriteLine(string.Join('-', args, 1));
            var tempChannel = channel.Guild.Channels.FirstOrDefault(x => x.Name == string.Join('-', args.Skip(1)).ToLower());
            tempChannel = tempChannel ?? channel.Guild.Channels.FirstOrDefault(x => x.Name.Contains(string.Join('-', args.Skip(1)).ToLower()));

            if (tempChannel is null) // If we found no matching channels
                return channel.SendMessageAsync("The specified channel could not be found. Please try again.");
            if (!(tempChannel is SocketTextChannel)) // If the channel found was not a valid text channel
                return channel.SendMessageAsync("The specified channel was not a text channel");

            guild.GuildSettings.WelcomeMessage.ChannelId = tempChannel.Id;
            guild.UserConfiguration[userId] = MessageBox.WelcomeSettings;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.WelcomeSettings);
        }

        private static Task<RestUserMessage> WelcomeSetMessage(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (string.IsNullOrWhiteSpace(args.ElementAtOrDefault(1)))
                return channel.SendMessageAsync(GetMissingParam("ChannelName", typeof(string)));

            string input = string.Join(' ', args.Skip(1));
            if (input.Length > 1900)
                return channel.SendMessageAsync("You cannot put in a message over 1900 characters. " +
                                                "The bot limits this as a saftey net so you don't go to close to Discord's native 2000 character limit.");

            guild.GuildSettings.WelcomeMessage.Message = input;
            guild.UserConfiguration[userId] = MessageBox.WelcomeSettings;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.WelcomeSettings);
        }

        private static Task<RestUserMessage> WelcomePreviewMessage(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (string.IsNullOrWhiteSpace(guild.GuildSettings.WelcomeMessage.Message))
                return channel.SendMessageAsync("There is currently no message set.");

            Task.Run(async () =>
            {
                await channel.SendMessageAsync(guild.GuildSettings.WelcomeMessage.Message
                .Replace("%SERVER%", channel.Guild.Name)
                .Replace("%USER%", "Raven"));
            });
            guild.UserConfiguration[userId] = MessageBox.WelcomeSettings;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.WelcomeSettings);
        }

        private static Task<RestUserMessage> GoodbyeSetChannel(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null) // If they didn't provide a channel name
                return channel.SendMessageAsync(GetMissingParam("ChannelName", typeof(string)));

            Console.WriteLine(string.Join('-', args, 1));
            var tempChannel = channel.Guild.Channels.FirstOrDefault(x => x.Name == string.Join('-', args.Skip(1)).ToLower());
            tempChannel = tempChannel ?? channel.Guild.Channels.FirstOrDefault(x => x.Name.Contains(string.Join('-', args.Skip(1)).ToLower()));

            if (tempChannel is null) // If we found no matching channels
                return channel.SendMessageAsync("The specified channel could not be found. Please try again.");
            if (!(tempChannel is SocketTextChannel)) // If the channel found was not a valid text channel
                return channel.SendMessageAsync("The specified channel was not a text channel");

            guild.GuildSettings.GoodbyeMessage.ChannelId = tempChannel.Id;
            guild.UserConfiguration[userId] = MessageBox.GoodbyeSettings;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.GoodbyeSettings);
        }

        private static Task<RestUserMessage> GoodbyeSetMessage(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (string.IsNullOrWhiteSpace(args.ElementAtOrDefault(1)))
                return channel.SendMessageAsync(GetMissingParam("ChannelName", typeof(string)));

            string input = string.Join(' ', args.Skip(1));
            if (input.Length > 1900)
                return channel.SendMessageAsync("You cannot put in a message over 1900 characters. " +
                                                "The bot limits this as a saftey net so you don't go to close to Discord's native 2000 character limit.");

            guild.GuildSettings.GoodbyeMessage.Message = input;
            guild.UserConfiguration[userId] = MessageBox.GoodbyeSettings;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.GoodbyeSettings);
        }

        private static Task<RestUserMessage> GoodbyePreviewMessage(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (string.IsNullOrWhiteSpace(guild.GuildSettings.GoodbyeMessage.Message))
                return channel.SendMessageAsync("There is currently no message set.");

            Task.Run(async () =>
            {
                await channel.SendMessageAsync(guild.GuildSettings.GoodbyeMessage.Message
                .Replace("%SERVER%", channel.Guild.Name)
                .Replace("%USER%", "Raven"));
            });
            guild.UserConfiguration[userId] = MessageBox.GoodbyeSettings;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.GoodbyeSettings);
        }
    }
}
