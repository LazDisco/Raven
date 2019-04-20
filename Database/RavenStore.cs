using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Discord;
using Microsoft.Extensions.Configuration;
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
                    // ReSharper disable once ForCanBeConvertedToForeach will crash if we use foreach
                    for (var index = 0; index < guilds.Count; index++)
                    {
                        var guild = guilds[index];
                        ulong.TryParse(session.Advanced.GetDocumentId(guild), out ulong id);
                        if (id != 0)
                        {
                            guild.GuildId = id;
                            for (var i = 0; i < guild.Users.Count; i++)
                            {
                                RavenUser user = guild.Users[i];
                                ulong.TryParse(user.UserIdString, out ulong userId);
                                if (userId != 0)
                                    user.UserId = userId;
                                else
                                {
                                    Logger.Log($"User {user.Username}#{user.Discriminator} ({user.UserIdString}) cannot be parsed.",
                                        "RavenDB",
                                        LogSeverity.Warning);
                                    continue;
                                }

                                guild.Users[i] = user;
                            }
                        }
                        else
                        {
                            #pragma warning disable CS4014
                            Logger.AbortAfterLog($"Guild Document {guild.Name} ({session.Advanced.GetDocumentId(guild)}) cannot be parsed.", "RavenDB",
                                LogSeverity.Error);
                            #pragma warning restore CS4014
                        }

                        guilds[index] = guild;
                    }
                    RavenDb.SetGuilds(guilds);
                } catch { RavenDb.SetGuilds(new List<RavenGuild>()); }

                try
                {
                    List<RavenUser> users = session.Query<RavenUser>().ToList();
                    // ReSharper disable once ForCanBeConvertedToForeach will crash if we use foreach
                    for (var index = 0; index < users.Count; index++)
                    {
                        var user = users[index];
                        ulong.TryParse(session.Advanced.GetDocumentId(user), out ulong id);
                        if (id != 0)
                            user.UserId = id;
                        else
                        {
                            #pragma warning disable CS4014
                            Logger.AbortAfterLog($"User Document {user.Username} ({session.Advanced.GetDocumentId(user)}) cannot be parsed.", "RavenDB",
                                LogSeverity.Error);
                            #pragma warning restore CS4014
                        }

                        users[index] = user;
                    }
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
            }.Initialize();

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
}
