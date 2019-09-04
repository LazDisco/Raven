using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Discord;
using Newtonsoft.Json;
using Raven.Services;

namespace Raven.Utilities
{
    public static class GlobalConfig
    {
        public static string Prefix { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public static string Token { get; private set; }
        public static string DbUrl { get; set; }
        public static string DbName { get; set; }
        public static int Shards { get; set; }
        /// <summary>An array of users that are registered at bot owners. These users surpass all permission requirements.</summary>
        public static ulong[] OwnerIds { get; set; }
        public static int MinGlobalXpGeneration { get; set; }
        public static int MaxGlobalXpGeneration { get; set; }
        public static uint MinTimeBetweenXpGeneration { get; set; }
        public static bool RunPluginFunctionsAsynchronously { get; set; }

        /// <summary>The location of our Certification file.</summary>
        public static string CertificationLocation { get; set; }
        /// <summary>The amount of XP we multiply by each level</summary>
        public static byte IncrementalLevelXpMultiplyer { get; set; }

        public static List<PluginInfo> PluginInfo { get; } = new List<PluginInfo>();

        public static void LoadConfig(GlobalConfigInstance instance)
        {
            // Get an array of all the properties
            PropertyInfo[] props = instance.GetType().GetProperties();

            // Get only the static properties
            PropertyInfo[] staticProps = typeof(GlobalConfig).GetProperties(BindingFlags.Public | BindingFlags.Static);

            // Iterate over our properties
            foreach (var prop in props)
            {
                // Find matching static properties
                var destinationProp = staticProps.Single(p => p.Name == prop.Name);

                // Set the static property value
                destinationProp.SetValue(null, prop.GetValue(instance));
            }

            if (OwnerIds is null)
                OwnerIds = new ulong[0];
        }
    }

    public class GlobalConfigInstance
    {
        public string Prefix { get; set; }
        public string Token { get; set; }
        public string DbUrl { get; set; }
        public string DbName { get; set; }
        public string CertificationLocation { get; set; }
        public int? Shards { get; set; }
        public ulong[] OwnerIds { get; set; }
        public int? MinGlobalXpGeneration { get; set; }
        public int? MaxGlobalXpGeneration { get; set; }
        public uint? MinTimeBetweenXpGeneration { get; set; }
        public byte? IncrementalLevelXpMultiplyer { get; set; }
        public bool RunPluginFunctionsAsynchronously { get; set; }

        public static GlobalConfigInstance GetInstance(string path)
        {
            string json = File.ReadAllText(path);
            GlobalConfigInstance instance = JsonConvert.DeserializeObject<GlobalConfigInstance>(json);
            
            instance.DbUrl = string.IsNullOrWhiteSpace(instance.DbUrl) ? "http://localhost:8080" : instance.DbUrl;
            instance.DbName = string.IsNullOrWhiteSpace(instance.DbName) ? "Raven" : instance.DbName;
            instance.Prefix = string.IsNullOrWhiteSpace(instance.Prefix) ? "|" : instance.Prefix;
            instance.CertificationLocation = string.IsNullOrEmpty(instance.CertificationLocation) ? "" : instance.CertificationLocation;

            if (string.IsNullOrWhiteSpace(instance.Token))
                throw new InvalidOperationException("Discord token is not present. Please see AppConfig.json and ensure the \"token:\" field is correct.");
            if (instance.Shards is null || instance.Shards <= 0)
                instance.Shards = 2;

            if (instance.MinGlobalXpGeneration is null || instance.MinGlobalXpGeneration is 0)
            {
                instance.MinGlobalXpGeneration = 2;
                Logger.Log("Minimum XP was null or 0. Defaulting to 2.", "AppConfig.json", LogSeverity.Warning);
            }

            if (instance.MaxGlobalXpGeneration is null || instance.MaxGlobalXpGeneration is 0)
            {
                instance.MaxGlobalXpGeneration = 5;
                Logger.Log("Maximum XP was null or 0. Defaulting to 5.", "AppConfig.json", LogSeverity.Warning);
            }

            if (instance.MinTimeBetweenXpGeneration is null || instance.MinTimeBetweenXpGeneration is 0)
            {
                instance.MinTimeBetweenXpGeneration = 60;
                Logger.Log("Min XP Time was null or 0. Defaulting to 60.", "AppConfig.json", LogSeverity.Warning);
            }

            if (instance.MinGlobalXpGeneration > instance.MaxGlobalXpGeneration)
            {
                instance.MinGlobalXpGeneration = instance.MaxGlobalXpGeneration;
                Logger.Log("Minimum XP was greater than the maximum. Defaulting to same value.", "AppConfig.json", LogSeverity.Warning);
            }

            else if (instance.MaxGlobalXpGeneration < instance.MinGlobalXpGeneration)
            {
                instance.MaxGlobalXpGeneration = instance.MinGlobalXpGeneration;
                Logger.Log("Maximum XP was below the minimum. Defaulting to same value.", "AppConfig.json", LogSeverity.Warning);
            }

            if (instance.IncrementalLevelXpMultiplyer is null || instance.IncrementalLevelXpMultiplyer <= 0)
            {
                instance.IncrementalLevelXpMultiplyer = 5;
                Logger.Log("Incremental Level XP Multiplyer was invalid or less than 0. Defaulting to 5.", "AppConfig.json", LogSeverity.Warning);
            }

            return instance;
        }
    }
}
