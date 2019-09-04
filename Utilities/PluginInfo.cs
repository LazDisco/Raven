using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Raven.Utilities
{
    public abstract class PluginInfo
    {
        public string PluginName;
        public string Description;
        public string PluginAuthor;
        public Version PluginVersion;
        public List<string> ModuleNames;

        public Func<SocketMessage, Task> MessageReceivedAsync = null;
        public Func<SocketGuildUser, Task> GuildUserJoin = null;
        public Func<SocketGuildUser, Task> GuildUserLeave = null;
        public Func<SocketGuild, SocketGuild, Task> GuildUpdate = null;

        protected PluginInfo(Type t)
        {
            Assembly a = Assembly.GetAssembly(t);
            this.PluginName = Path.GetFileName(a.CodeBase);

            FileVersionInfo v = FileVersionInfo.GetVersionInfo(a.Location);
            this.PluginVersion = new Version(v.FileMajorPart, v.FileMinorPart, v.FileBuildPart);

            this.ModuleNames = new List<string>();
            foreach (Type type in Assembly.GetAssembly(t).GetExportedTypes())
                if (type.BaseType == (typeof(ModuleBase<SocketCommandContext>)))
                    ModuleNames.Add(type.Name);
        }
    }
}
