using System;
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
    public static class Utils
    {
        /// <summary>Returns a string that is formatted in a Gauss code block. Used for config menus mostly.</summary>
        public static string GetCodeBlock(string contents)
        {
            return "```gauss\n" + contents + "\n\n# Specify an option by typing the number next to it.\n" +
                   "# You can exit the menu by typing 'exit'. Regular commands will not work while in the menu.```";
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
        public static Task<RestUserMessage> SelectSubMenu(RavenGuild guild, ulong userId, ISocketMessageChannel channel,
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

                case MessageBox.LsSettingSubmenu:
                    guild.UserConfiguration[userId] = MessageBox.LsSettingSubmenu;
                    guild.Save();
                    return channel.SendMessageAsync(GetCodeBlock(
                        File.ReadAllText($@"{Directory.GetCurrentDirectory()}/ConfigTextFiles/{MenuFiles.LsSubSettings}.txt"))
                        .Replace("%CurrentSetting%", SplitPascalCase(guild.GuildSettings.LevelConfig.LevelSettings.ToString())));

                default:
                    guild.UserConfiguration.Remove(userId);
                    guild.Save();
                    return channel.SendMessageAsync("I don't know how you got here, but I'm going to termiante this menu just in case.");
            }
        }

        /// <summary>Process the actual options</summary>
        public static Task<RestUserMessage> SelectOption(RavenGuild guild, ulong userId, ISocketMessageChannel channel, string[] args)
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

                case MessageBox.LsSettingSubmenu:
                    switch (option)
                    {
                        case MessageBox.LsSettingDisabled:
                            guild.GuildSettings.LevelConfig.LevelSettings = LevelSettings.Disabled;
                            guild.UserConfiguration.Remove(userId);
                            guild.Save();
                            return channel.SendMessageAsync("The level system has been completely disabled.");

                        case MessageBox.LsSettingGlobalLevel:
                            guild.GuildSettings.LevelConfig.LevelSettings = LevelSettings.GlobalLeveling;
                            guild.UserConfiguration.Remove(userId);
                            guild.Save();
                            return channel.SendMessageAsync("The level system will only apply on a global level.");

                        case MessageBox.LsSettingGuildLevel:
                            guild.GuildSettings.LevelConfig.LevelSettings = LevelSettings.GuildLeveling;
                            guild.UserConfiguration.Remove(userId);
                            guild.Save();
                            return channel.SendMessageAsync("The level system has been completely enabled.");

                        default:
                            return InvalidOption(channel);
                    }

                default:
                    guild.UserConfiguration.Remove(userId);
                    guild.Save();
                    return channel.SendMessageAsync("I don't know how you got here, but I am gonna exit the menu just to be safe.");
            }
        }

        //private static Task<RestUserMessage> TEMPLATEFUNCTION(RavenGuild guild, ulong userId, ISocketMessageChannel channel, string[] args) {}

        private static Task<RestUserMessage> LsSetMinXp(RavenGuild guild, ulong userId, ISocketMessageChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("MinimumXp", typeof(int)));
            if (!int.TryParse(args.ElementAt(1), out int val))
                return channel.SendMessageAsync(ParamWrongFormat("MinimumXp", typeof(int)));
            // TODO: Unhardcode clamped global xp values
            if (val <= 0)
                return channel.SendMessageAsync("MinimumXp must be greater than 0.");
            if (val < 999)
                return channel.SendMessageAsync("MinimumXp must not be greater than 999.");

            guild.GuildSettings.LevelConfig.MinXpGenerated = val;
            guild.UserConfiguration.Remove(userId);
            guild.Save();
            return channel.SendMessageAsync($"Minimum XP generated per generation set to {val}.");
        }

        private static Task<RestUserMessage> LsSetMaxXp(RavenGuild guild, ulong userId, ISocketMessageChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("MaximumXp", typeof(int)));
            if (!int.TryParse(args.ElementAt(1), out int val))
                return channel.SendMessageAsync(ParamWrongFormat("MaximumXp", typeof(int)));
            // TODO: Unhardcode clamped global xp values
            if (val <= 1)
                return channel.SendMessageAsync("MaximumXp must be greater than 1.");
            if (val < 1000)
                return channel.SendMessageAsync("MaximumXp must not be greater than 1000.");

            guild.GuildSettings.LevelConfig.MaxXpGenerated = val;
            guild.UserConfiguration.Remove(userId);
            guild.Save();
            return channel.SendMessageAsync($"Maximum XP generated per generation set to {val}.");
        }

        private static Task<RestUserMessage> LsSetMinXpTime(RavenGuild guild, ulong userId, ISocketMessageChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("MinXpTime", typeof(int)));
            if (!int.TryParse(args.ElementAt(1), out int val))
                return channel.SendMessageAsync(ParamWrongFormat("MinXpTime", typeof(int)));
            // TODO: Unhardcode clamped global xp values
            if (val <= 30)
                return channel.SendMessageAsync("MinXpTime must be greater than or equal to 30.");
            if (val < 180)
                return channel.SendMessageAsync("MinXpTime must not be greater than 180.");

            guild.GuildSettings.LevelConfig.SecondsBetweenXpGiven = (uint)val;
            guild.UserConfiguration.Remove(userId);
            guild.Save();
            return channel.SendMessageAsync($"XP Generation Interval set to {val}.");
        }


    }
}
