using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Discord.Commands;

namespace Raven.Utilities
{
    public abstract class PluginInfo
    {
        public string PluginName;
        public string Description;
        public string PluginAuthor;
        public Version PluginVersion;
        public List<string> ModuleNames;

        protected PluginInfo(Type t)
        {
            this.ModuleNames = new List<string>();
            foreach (Type type in Assembly.GetAssembly(t).GetExportedTypes())
                if (type.IsAssignableFrom(typeof(ModuleBase<SocketCommandContext>)))
                    ModuleNames.Add(type.Name);
        }
    }
}
