using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                case MessageBox.BaseMenu:
                {
                    guild.UserConfiguration[userId] = MessageBox.BaseMenu;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(File.ReadAllText(
                        $"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.BaseMenu.ToString()}.txt")));
                }

                case MessageBox.LevelSettings:
                {
                    guild.UserConfiguration[userId] = MessageBox.LevelSettings;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(
                            File.ReadAllText(
                                $@"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.LevelSettings}.txt"))
                        .Replace("%MinXP%", guild.GuildSettings.LevelConfig.MinXpGenerated.ToString())
                        .Replace("%MaxXP%", guild.GuildSettings.LevelConfig.MaxXpGenerated.ToString())
                        .Replace("%XPTime%", guild.GuildSettings.LevelConfig.SecondsBetweenXpGiven.ToString()));
                }

                case MessageBox.WelcomeSettings:
                {
                    guild.UserConfiguration[userId] = MessageBox.WelcomeSettings;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(
                            File.ReadAllText(
                                $@"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.WelcomeSettings}.txt"))
                        .Replace("%state%", guild.GuildSettings.WelcomeMessage.Enabled ? "Enabled" : "Disabled")
                        .Replace("%channel%", guild.GuildSettings.WelcomeMessage.ChannelId == null
                            ? "Not Set"
                            : channel.Guild.GetTextChannel(guild.GuildSettings.WelcomeMessage.ChannelId.Value) == null
                                ? "DELETED CHANNEL"
                                : "#" + channel.Guild.GetTextChannel(guild.GuildSettings.WelcomeMessage.ChannelId.Value)
                                      .Name));
                }

                case MessageBox.GoodbyeSettings:
                {
                    guild.UserConfiguration[userId] = MessageBox.GoodbyeSettings;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(
                            File.ReadAllText(
                                $@"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.GoodbyeSettings}.txt"))
                        .Replace("%state%", guild.GuildSettings.GoodbyeMessage.Enabled ? "Enabled" : "Disabled")
                        .Replace("%channel%", guild.GuildSettings.GoodbyeMessage.ChannelId == null
                            ? "Not Set"
                            : channel.Guild.GetTextChannel(guild.GuildSettings.GoodbyeMessage.ChannelId.Value) == null
                                ? "DELETED CHANNEL"
                                : "#" + channel.Guild.GetTextChannel(guild.GuildSettings.GoodbyeMessage.ChannelId.Value)
                                      .Name));
                }

                case MessageBox.LoggingSettings:
                {
                    guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(
                            File.ReadAllText(
                                $@"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.LoggingSettings}.txt"))
                        .Replace("%state%", guild.LoggingSettings.Enabled ? "Enabled" : "Disabled")
                        .Replace("%join%", guild.LoggingSettings.Join ? "Enabled" : "Disabled")
                        .Replace("%leave%", guild.LoggingSettings.Leave ? "Enabled" : "Disabled")
                        .Replace("%ban%", guild.LoggingSettings.Ban ? "Enabled" : "Disabled")
                        .Replace("%msg%", guild.LoggingSettings.Msg ? "Enabled" : "Disabled")
                        .Replace("%user%", guild.LoggingSettings.User ? "Enabled" : "Disabled")
                        .Replace("%role%", guild.LoggingSettings.Role ? "Enabled" : "Disabled")
                        .Replace("%vc%", guild.LoggingSettings.VoiceChannel ? "Enabled" : "Disabled")
                        .Replace("%guild%", guild.LoggingSettings.Join ? "Enabled" : "Disabled")
                        .Replace("%channel%", guild.LoggingSettings.ChannelId == null
                            ? "Not Set"
                            : channel.Guild.GetTextChannel(guild.LoggingSettings.ChannelId.Value) == null
                                ? "DELETED CHANNEL"
                                : "#" + channel.Guild.GetTextChannel(guild.LoggingSettings.ChannelId.Value)
                                      .Name));
                }

                case MessageBox.GeneralSettings:
                {
                    guild.UserConfiguration[userId] = MessageBox.GeneralSettings;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(
                            File.ReadAllText(
                                $@"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.GeneralSettings}.txt"))
                            .Replace("%prefix%", guild.GuildSettings.Prefix)
                            .Replace("%invite%", guild.GuildSettings.AutoblockInviteLinks ? "Enabled" : "Disabled"));
                }
                // Sub menus

                case MessageBox.LsSettingSubmenu:
                {
                    guild.UserConfiguration[userId] = MessageBox.LsSettingSubmenu;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(
                            File.ReadAllText(
                                $@"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.LsSubSettings}.txt"))
                        .Replace("%CurrentSetting%",
                            SplitPascalCase(guild.GuildSettings.LevelConfig.LevelSettings.ToString())));
                }

                case MessageBox.GeneralConfigureDisallowedModules:
                {
                    guild.UserConfiguration[userId] = MessageBox.GeneralConfigureDisallowedModules;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock("This menu is not finished. Please return to the previous section."));
                }

                case MessageBox.GeneralConfigureDisallowedCommands:
                {
                    guild.UserConfiguration[userId] = MessageBox.GeneralConfigureDisallowedCommands;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock("This menu is not finished. Please return to the previous section."));
                }

                case MessageBox.GeneralConfigureBlacklistedChannels:
                {
                    guild.UserConfiguration[userId] = MessageBox.GeneralConfigureBlacklistedChannels;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(File.ReadAllText(
                                $@"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.GeneralBlacklist}.txt"))
                        .Replace("%type%", "Channel"));
                    }

                case MessageBox.GeneralConfigureBlacklistedRoles:
                {
                    guild.UserConfiguration[userId] = MessageBox.GeneralConfigureBlacklistedRoles;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(File.ReadAllText(
                            $@"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.GeneralBlacklist}.txt"))
                        .Replace("%type%", "Role"));
                    }

                case MessageBox.GeneralConfigureBlacklistedUsers:
                {
                    guild.UserConfiguration[userId] = MessageBox.GeneralConfigureBlacklistedUsers;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(File.ReadAllText(
                            $@"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.GeneralBlacklist}.txt"))
                        .Replace("%type%", "User"));
                    }

                default:
                    guild.UserConfiguration.Remove(userId);
                    guild.Save();
                    return channel.SendMessageAsync("I don't know how you got here, but I'm going to termiante this menu just in case.");
            }
        }

        /// <summary>Process the actual options</summary>
        public static Task<RestUserMessage> SelectOption(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (!uint.TryParse(args[0], out uint temp))
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
                    if ((int) option is 7) // We need to cast our submenus to a higher value otherwise they cause problems when selecting options
                        option = MessageBox.LsSettingSubmenu;
                    switch (option)
                    {
                        case MessageBox.LsSetMinXp:
                            return LsSetMinXp(guild, userId, channel, args);

                        case MessageBox.LsSetMaxXp:
                            return LsSetMaxXp(guild, userId, channel, args);

                        case MessageBox.LsSetXpTime:
                            return LsSetMinXpTime(guild, userId, channel, args);

                        case MessageBox.LsAddLevelBinding:
                            return LsAddLevelBinding(guild, userId, channel, args);

                        case MessageBox.LsRemoveLevelBinding:
                            return LsRemoveLevelBinding(guild, userId, channel, args);

                        case MessageBox.LsListLevelBindings:
                            return LsListLevelBindings(guild, userId, channel, args);

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
                            guild.GuildSettings.WelcomeMessage.Enabled = !guild.GuildSettings.WelcomeMessage.Enabled;
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

                // Goodbye Settings Submenu
                case MessageBox.GoodbyeSettings:
                {
                    switch (option)
                    {
                        case MessageBox.GoodbyeToggle:
                            guild.GuildSettings.GoodbyeMessage.Enabled = !guild.GuildSettings.GoodbyeMessage.Enabled;
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

                // Welcome Message Settings Sub Menu
                case MessageBox.LoggingSettings:
                {
                    switch (option)
                    {
                        case MessageBox.LoggingModuleEnabled:
                            guild.LoggingSettings.Enabled = !guild.LoggingSettings.Enabled;
                            guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                            guild.Save();
                            return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                        case MessageBox.LoggingChannel:
                            return LoggingSetChannel(guild, userId, channel, args);

                        case MessageBox.LoggingEnableAll:
                            guild.LoggingSettings.Enabled = true;
                            guild.LoggingSettings.Join = true;
                            guild.LoggingSettings.Leave = true;
                            guild.LoggingSettings.Ban = true;
                            guild.LoggingSettings.Msg = true;
                            guild.LoggingSettings.User = true;
                            guild.LoggingSettings.VoiceChannel = true;
                            guild.LoggingSettings.Role = true;
                            guild.LoggingSettings.GuildUpdate = true;
                            guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                            guild.Save();
                            return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                        case MessageBox.LoggingDisableAll:
                            guild.LoggingSettings.Enabled = false;
                            guild.LoggingSettings.Join = false;
                            guild.LoggingSettings.Leave = false;
                            guild.LoggingSettings.Ban = false;
                            guild.LoggingSettings.Msg = false;
                            guild.LoggingSettings.User = false;
                            guild.LoggingSettings.VoiceChannel = false;
                            guild.LoggingSettings.Role = false;
                            guild.LoggingSettings.GuildUpdate = false;
                            guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                            guild.Save();
                            return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                        case MessageBox.LoggingJoin:
                            guild.LoggingSettings.Join = !guild.LoggingSettings.Join;
                            guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                            guild.Save();
                            return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                        case MessageBox.LoggingLeave:
                            guild.LoggingSettings.Leave = !guild.LoggingSettings.Leave;
                            guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                            guild.Save();
                            return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                        case MessageBox.LoggingBan:
                            guild.LoggingSettings.Ban = !guild.LoggingSettings.Ban;
                            guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                            guild.Save();
                            return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                        case MessageBox.LoggingMsg:
                            guild.LoggingSettings.Msg = !guild.LoggingSettings.Msg;
                            guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                            guild.Save();
                            return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                        case MessageBox.LoggingUser:
                            guild.LoggingSettings.User = !guild.LoggingSettings.User;
                            guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                            guild.Save();
                            return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                        case MessageBox.LoggingRole:
                            guild.LoggingSettings.Role = !guild.LoggingSettings.Role;
                            guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                            guild.Save();
                            return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                        case MessageBox.LoggingVc:
                            guild.LoggingSettings.VoiceChannel = !guild.LoggingSettings.VoiceChannel;
                            guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                            guild.Save();
                            return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                        case MessageBox.LoggingGuildUpdate:
                            guild.LoggingSettings.GuildUpdate = !guild.LoggingSettings.GuildUpdate;
                            guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                            guild.Save();
                            return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                        default:
                            return InvalidOption(channel);
                    }
                }

                // Generic Settings (prefix, blacklists, module control, and probably more in the future)
                case MessageBox.GeneralSettings:
                {
                    switch ((int) option)
                    {
                        case 3:
                            option = MessageBox.GeneralConfigureDisallowedModules;
                            break;
                        case 4:
                            option = MessageBox.GeneralConfigureDisallowedCommands;
                            break;
                        case 5:
                            option = MessageBox.GeneralConfigureBlacklistedChannels;
                            break;
                        case 6:
                            option = MessageBox.GeneralConfigureBlacklistedRoles;
                            break;
                        case 7:
                            option = MessageBox.GeneralConfigureBlacklistedUsers;
                            break;
                    }

                    switch (option)
                    {
                        case MessageBox.GeneralSetPrefix:
                            return GeneralSetPrefix(guild, userId, channel, args);

                        case MessageBox.GeneralToggleInviteBlocking:
                            guild.GuildSettings.AutoblockInviteLinks = !guild.GuildSettings.AutoblockInviteLinks;
                            guild.UserConfiguration[userId] = MessageBox.GeneralSettings;
                            guild.Save();
                            return SelectSubMenu(guild, userId, channel, MessageBox.GeneralSettings);

                        case MessageBox.GeneralConfigureDisallowedModules:
                        case MessageBox.GeneralConfigureDisallowedCommands:
                        case MessageBox.GeneralConfigureBlacklistedChannels:
                        case MessageBox.GeneralConfigureBlacklistedRoles:
                        case MessageBox.GeneralConfigureBlacklistedUsers:
                            return SelectSubMenu(guild, userId, channel, option);

                        default:
                            return InvalidOption(channel);
                    }
                }

                case MessageBox.GeneralConfigureBlacklistedChannels:
                {
                    switch (option)
                    {
                        case MessageBox.BlacklistAddTo:
                            return BlacklistAddChannel(guild, userId, channel, args);

                        case MessageBox.BlacklistRemoveFrom:
                            return BlacklistRemoveChannel(guild, userId, channel, args);

                        case MessageBox.BlacklistDisplay:
                            return BlacklistDisplayChannels(guild, userId, channel, args);

                        default:
                            return InvalidOption(channel);
                    }
                }

                case MessageBox.GeneralConfigureBlacklistedRoles:
                {
                    switch (option)
                    {
                        case MessageBox.BlacklistAddTo:
                            return BlacklistAddRole(guild, userId, channel, args);

                        case MessageBox.BlacklistRemoveFrom:
                            return BlacklistRemoveRole(guild, userId, channel, args);

                        case MessageBox.BlacklistDisplay:
                            return BlacklistDisplayRoles(guild, userId, channel, args);

                        default:
                            return InvalidOption(channel);
                    }
                }

                case MessageBox.GeneralConfigureBlacklistedUsers:
                {
                    switch (option)
                    {
                        case MessageBox.BlacklistAddTo:
                            return BlacklistAddUser(guild, userId, channel, args);

                        case MessageBox.BlacklistRemoveFrom:
                            return BlacklistRemoveUser(guild, userId, channel, args);

                        case MessageBox.BlacklistDisplay:
                            return BlacklistDisplayUsers(guild, userId, channel, args);

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

        private static Task<RestUserMessage> LsAddLevelBinding(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("Level", typeof(byte)));
            if (!byte.TryParse(args.ElementAt(1), out byte val))
                return channel.SendMessageAsync(ParamWrongFormat("Level", typeof(byte)));
            if (val <= 1)
                return channel.SendMessageAsync("You cannot bind a rank to level 1 or below.");
            if (guild.GuildSettings.LevelConfig.RankBindings.ContainsKey(val))
                return channel.SendMessageAsync($"Level {val} already has a binding assigned to it.");

            if (string.IsNullOrWhiteSpace(args.ElementAtOrDefault(2)))
                return channel.SendMessageAsync(GetMissingParam("RankName", typeof(string)));

            string value = string.Join(' ', args.Skip(2));
            if (value.Length > 32)
                return channel.SendMessageAsync("Rank name cannot be over 32 characters.");

            guild.GuildSettings.LevelConfig.RankBindings[val] = value;
            guild.UserConfiguration[userId] = MessageBox.LevelSettings;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.LevelSettings);
        }

        private static Task<RestUserMessage> LsRemoveLevelBinding(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("Level", typeof(byte)));
            if (!byte.TryParse(args.ElementAt(1), out byte val))
                return channel.SendMessageAsync(ParamWrongFormat("Level", typeof(byte)));
            if (val <= 1)
                return channel.SendMessageAsync("There are no bindings for level 1 or below.");

            if (!guild.GuildSettings.LevelConfig.RankBindings.ContainsKey(val))
                return channel.SendMessageAsync("There is no rank bound to the specified level.");

            guild.GuildSettings.LevelConfig.RankBindings.Remove(val);
            guild.UserConfiguration[userId] = MessageBox.LevelSettings;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.LevelSettings);
        }

        private static Task<RestUserMessage> LsListLevelBindings(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (guild.GuildSettings.LevelConfig.RankBindings.Count == 0)
                return channel.SendMessageAsync("There are no bindings currently set.");

            List<string> ranks = new List<string>() { "" };
            int i = 0;
            foreach (var rank in guild.GuildSettings.LevelConfig.RankBindings)
            {
                string value = $"{rank.Key}: {rank.Value}\n";
                ranks[i] += value;
                if (ranks[i].Length <= 1900) continue;
                ranks.Add("");
                i++;
            }

            var ii = 1;
            while (ii < ranks.Count)
            {
                channel.SendMessageAsync(ranks[ii - 1]);
                ii++;
            }

            return channel.SendMessageAsync(ranks[ii - 1]);
        }

        private static Task<RestUserMessage> WelcomeSetChannel(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null) // If they didn't provide a channel name
                return channel.SendMessageAsync(GetMissingParam("ChannelName", typeof(string)));

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
                return channel.SendMessageAsync(GetMissingParam("Message", typeof(string)));

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
                return channel.SendMessageAsync(GetMissingParam("Message", typeof(string)));

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

        private static Task<RestUserMessage> LoggingSetChannel(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
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

            guild.LoggingSettings.ChannelId = tempChannel.Id;
            guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);
        }

        private static Task<RestUserMessage> GeneralSetPrefix(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null) // If they didn't provide a channel name
                return channel.SendMessageAsync(GetMissingParam("Prefix", typeof(string)));

            guild.GuildSettings.Prefix = string.Join(' ', args.Skip(1));
            guild.UserConfiguration[userId] = MessageBox.GeneralSettings;
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.GeneralSettings);
        }

        private static Task<RestUserMessage> BlacklistAddChannel(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null) // If they didn't provide a channel name
                return channel.SendMessageAsync(GetMissingParam("ChannelName", typeof(string)));

            var tempChannel = channel.Guild.Channels.FirstOrDefault(x => x.Name == string.Join('-', args.Skip(1)).ToLower());
            tempChannel = tempChannel ?? channel.Guild.Channels.FirstOrDefault(x => x.Name.Contains(string.Join('-', args.Skip(1)).ToLower()));

            if (tempChannel is null) // If we found no matching channels
                return channel.SendMessageAsync("The specified channel could not be found. Please try again.");
            if (!(tempChannel is SocketTextChannel)) // If the channel found was not a valid text channel
                return channel.SendMessageAsync("The specified channel was not a text channel");

            guild.GuildSettings.BlacklistedChannels.Add(tempChannel.Id);
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureBlacklistedChannels);
        }

        private static Task<RestUserMessage> BlacklistRemoveChannel(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null) // If they didn't provide a channel name
                return channel.SendMessageAsync(GetMissingParam("ChannelId", typeof(ulong)));

            if (!ulong.TryParse(args.ElementAtOrDefault(1), out ulong id))
                return channel.SendMessageAsync(ParamWrongFormat("ChannelId", typeof(ulong)));

            if (guild.GuildSettings.BlacklistedChannels.Any(x => x == id))
            {
                guild.GuildSettings.BlacklistedChannels.Remove(id);
                guild.Save();
                return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureBlacklistedChannels);
            }

            else
                return channel.SendMessageAsync("The specified channel was not blacklisted.");
        }

        private static Task<RestUserMessage> BlacklistDisplayChannels(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (guild.GuildSettings.BlacklistedChannels.Count == 0)
                return channel.SendMessageAsync("There are currently no blacklisted channels.");

            string channels = "Blacklisted Channels:\n";

            foreach (ulong i in guild.GuildSettings.BlacklistedChannels)
            {
                var tempChannel = channel.Guild.Channels.FirstOrDefault(x => x.Id == i);

                if (tempChannel is null)
                    channels += $"#INVALID-CHANNEL ({i})\n";
                else
                    channels += $"#{tempChannel.Name} ({i})\n";
            }

            if (channels.Length > 1900)
                return channel.SendMessageAsync("Too many channels to list. Cutting off after 1800 characters.\n"
                                                + channels.Substring(0, 1800));
            else
                return channel.SendMessageAsync("```cs\n" + channels + "\n```");
        }

        private static Task<RestUserMessage> BlacklistAddRole(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null) // If they didn't provide a role name
                return channel.SendMessageAsync(GetMissingParam("RoleName", typeof(string)));

            var tempRole = channel.Guild.Roles.FirstOrDefault(x => x.Name == string.Join(' ', args.Skip(1)));
            tempRole = tempRole ?? channel.Guild.Roles.FirstOrDefault(x => x.Name.Contains(string.Join(' ', args.Skip(1))));

            if (tempRole is null) // If we found no matching channels
                return channel.SendMessageAsync("The specified role could not be found. Please try again.");

            guild.GuildSettings.BlacklistedRoles.Add(tempRole.Id);
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureBlacklistedRoles);
        }

        private static Task<RestUserMessage> BlacklistRemoveRole(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null) // If they didn't provide a channel name
                return channel.SendMessageAsync(GetMissingParam("RoleId", typeof(ulong)));

            if (!ulong.TryParse(args.ElementAtOrDefault(1), out ulong id))
                return channel.SendMessageAsync(ParamWrongFormat("RoleId", typeof(ulong)));

            if (guild.GuildSettings.BlacklistedRoles.Any(x => x == id))
            {
                guild.GuildSettings.BlacklistedRoles.Remove(id);
                guild.Save();
                return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureBlacklistedRoles);
            }

            else
                return channel.SendMessageAsync("The specified role was not blacklisted.");
        }

        private static Task<RestUserMessage> BlacklistDisplayRoles(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (guild.GuildSettings.BlacklistedRoles.Count == 0)
                return channel.SendMessageAsync("There are currently no blacklisted roles.");

            string roles = "Blacklisted Roles:\n";

            foreach (ulong i in guild.GuildSettings.BlacklistedRoles)
            {
                var tempRole = channel.Guild.Roles.FirstOrDefault(x => x.Id == i);

                if (tempRole is null)
                    roles += $"DELETED-ROLE ({i})\n";
                else
                    roles += $"{tempRole.Name} ({i})\n";
            }

            if (roles.Length > 1900)
                return channel.SendMessageAsync("Too many roles to list. Cutting off after 1800 characters.\n"
                                                + roles.Substring(0, 1800));
            else
                return channel.SendMessageAsync("```cs\n" + roles + "\n```");
        }

        private static Task<RestUserMessage> BlacklistAddUser(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("Username", typeof(string)));

            string name = string.Join(' ', args.Skip(1)).ToLower();

            var user = (channel.Guild.Users.FirstOrDefault(x => (x.Username + "#" + x.DiscriminatorValue).Contains(name)) ??
                        channel.Guild.Users.FirstOrDefault(x => x.Nickname.Contains(name))) ??
                        channel.Guild.Users.FirstOrDefault(x => x.Id.ToString() == name);

            if (user is null)
                return channel.SendMessageAsync("Could not find a matching user.");

            guild.GuildSettings.BlacklistedChannels.Add(user.Id);
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureBlacklistedUsers);
        }

        private static Task<RestUserMessage> BlacklistRemoveUser(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("UserId", typeof(ulong)));

            if (!ulong.TryParse(args.ElementAtOrDefault(1), out ulong id))
                return channel.SendMessageAsync(ParamWrongFormat("UserId", typeof(ulong)));

            if (guild.GuildSettings.BlacklistedUsers.Any(x => x == id))
            {
                guild.GuildSettings.BlacklistedUsers.Remove(id);
                guild.Save();
                return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureBlacklistedUsers);
            }

            else
                return channel.SendMessageAsync("The specified user was not blacklisted.");
        }

        private static Task<RestUserMessage> BlacklistDisplayUsers(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (guild.GuildSettings.BlacklistedChannels.Count == 0)
                return channel.SendMessageAsync("There are currently no blacklisted users.");

            string users = "Blacklisted Users:\n";

            foreach (ulong i in guild.GuildSettings.BlacklistedUsers)
            {
                var user = channel.Guild.Users.FirstOrDefault(x => x.Id == i);
                if (user is null)
                {
                    RavenUser deletedRavenUser = guild.Users.FirstOrDefault(x => x.UserId == i);
                    string oldUsername = deletedRavenUser == null ? "Unknown" : deletedRavenUser.Username + "#" + deletedRavenUser.Discriminator;
                    users += $"USER-LEFT (ID: {i} - Old Name: {oldUsername})\n";
                }
                else
                    users += $"{user.Username} ({i})\n";
            }

            if (users.Length > 1900)
                return channel.SendMessageAsync("Too many users to list. Cutting off after 1800 characters.\n"
                                                + users.Substring(0, 1800));
            else
                return channel.SendMessageAsync("```cs\n" + users + "\n```");
        }
    }
}
