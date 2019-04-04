using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Raven
{
    public static class GlobalConfig
    {
        public static string Prefix { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public static string Token { get; private set; }
        public static string DbUrl { get; set; }
        public static string DbName { get; set; }
        public static int? Shards { get; set; }
        /// <summary>An array of users that are registered at bot owners. These users surpass all permission requirements.</summary>
        public static ulong[] OwnerIds { get; set; }

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
        }
    }

    public class GlobalConfigInstance
    {
        public string Prefix { get; set; }
        public string Token { get; set; }
        public string DbUrl { get; set; }
        public string DbName { get; set; }
        public int? Shards { get; set; }
        public ulong[] OwnerIds { get; set; }

        public static GlobalConfigInstance GetInstance(string path)
        {
            string json = File.ReadAllText(path);
            GlobalConfigInstance instance = JsonConvert.DeserializeObject<GlobalConfigInstance>(json);
            
            instance.DbUrl = string.IsNullOrWhiteSpace(instance.DbUrl) ? "http://localhost:8080" : instance.DbUrl;
            instance.DbName = string.IsNullOrWhiteSpace(instance.DbName) ? "Raven" : instance.DbName;
            instance.Prefix = string.IsNullOrWhiteSpace(instance.Prefix) ? "|" : instance.Prefix;

            if (string.IsNullOrWhiteSpace(instance.Token))
                throw new InvalidOperationException("Discord token is not present. Please see AppConfig.json and ensure the \"token:\" field is correct.");
            if (instance.Shards is null || instance.Shards <= 0)
                instance.Shards = 2;

            return instance;
        }
    }
}
