using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Discord;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.Backups;
using Raven.Client.Documents.Session;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Raven.Services;

namespace Raven.Database
{
    public static class RavenStore
    {
        private static Lazy<IDocumentStore> store { get; set; }
        public static IDocumentStore Store => store.Value;

        public static void Initialise()
        {
            store = new Lazy<IDocumentStore>(GenerateStore());
            using (IDocumentSession session = store.Value.OpenSession())
            {
                try
                {
                    List<RavenGuild> guilds = session.Query<RavenGuild>().ToList();
                    RavenDb.SetGuilds(guilds);
                } catch { RavenDb.SetGuilds(new List<RavenGuild>()); }

                try
                {
                    List<RavenUser> users = session.Query<RavenUser>().ToList();
                    RavenDb.SetUsers(users);
                }
                catch { RavenDb.SetUsers(new List<RavenUser>()); }
            }
        }

        private static IDocumentStore GenerateStore()
        {
            Logger.Log("Generating RavenDB Store", "RavenDB", LogSeverity.Info);
            string databaseName = GlobalConfig.DbName;
            IDocumentStore Store = new DocumentStore()
            {
                Urls = new string[] { GlobalConfig.DbUrl },
                Database = databaseName
            };
            Store.Conventions.CustomizeJsonSerializer = UlongJsonSerializer.InitialiseSerializer;
            Store = Store.Initialize();

            // Try block to check if the database is actually running.
            try
            {
                // If the database specified in the config doesn't exist, we create it
                if (Store.Maintenance.Server.Send(new GetDatabaseNamesOperation(0, 2)).All(x => x != databaseName))
                    Store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(databaseName)));
            }

            catch (Exception ex)
            {
                #pragma warning disable 4014
                Logger.AbortAfterLog("Unable to establish connection to database.", "RavenDB", LogSeverity.Critical,

                    ex.Message.Split('\n')[0] + "\n" + ((ex.InnerException.Message) ?? ex.Message));
                Console.ReadLine();
                #pragma warning restore 4014
            }

            // We run backups to make sure everything works as intended
            DatabaseRecordWithEtag dbRecord = Store.Maintenance.Server.Send(new GetDatabaseRecordOperation(databaseName));
            PeriodicBackupConfiguration backupConfig = dbRecord.PeriodicBackups.FirstOrDefault(x => x.Name == "Default Backup");
            try
            {
                if (!Directory.Exists(Directory.GetCurrentDirectory() + @"/Backups"))
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"/Backups");

                if (backupConfig == null)
                {
                    Logger.Log("Backup Config Undefined. Generating Generic.", "RavenDB", LogSeverity.Warning);
                    PeriodicBackupConfiguration newConfig = new PeriodicBackupConfiguration()
                    {
                        Name = "Default Backup",
                        IncrementalBackupFrequency = "*/5 * * ***", // every five mins
                        FullBackupFrequency = "* */3 * * *", // Every 3 hours
                        LocalSettings = new LocalSettings() { FolderPath = Directory.GetCurrentDirectory() + @"/Backups" },
                        Disabled = false,
                        BackupType = BackupType.Backup
                    };
                    Store.Maintenance.ForDatabase(databaseName).Send(new UpdatePeriodicBackupOperation(newConfig));
                }

                else
                {
                    Logger.Log("Backup Config Loaded.", "RavenDB", LogSeverity.Info);
                    backupConfig.LocalSettings = new LocalSettings() { FolderPath = Directory.GetCurrentDirectory() + @"/Backups" };
                    Store.Maintenance.ForDatabase(databaseName).Send(new UpdatePeriodicBackupOperation(backupConfig));
                }
            }

            catch (Exception ex)
            {
                Logger.Log("Unable to setup RavenDB Automated backups. Backups will not be saved and errors could cause loss of data.", "RavenDB",
                    LogSeverity.Warning, ex.Message);
            }

            return Store;
        }
    }

    public class UlongJsonSerializer : JsonConverter
    {
        public static void InitialiseSerializer(JsonSerializer serializer) => serializer.Converters.Add(new UlongJsonSerializer());

        public override bool CanConvert(Type type)
        {
            if (type == typeof(ulong?))
                return true;

            return type == typeof(ulong);
        }

        public override object ReadJson(JsonReader reader, Type type, object value, JsonSerializer serializer) => reader.Value != null ? Convert.ToUInt64(reader.Value.ToString()) : value;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ulong ulongValue = Convert.ToUInt64(value);
            string stringRepresentation = ulongValue.ToString();
            writer.WriteValue(stringRepresentation);
        }
    }
}
