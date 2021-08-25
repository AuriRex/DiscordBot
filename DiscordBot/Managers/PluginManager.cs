using Communicator.Net;
using DiscordBot.Events;
using DiscordBotPluginBase.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace DiscordBot.Managers
{
    // https://docs.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support
    public class PluginManager
    {
        public delegate void CommunicationServiceRegistered(CommunicationServiceRegisteredArgs args);

        private List<Assembly> LoadedPlugins { get; set; } = new List<Assembly>();
        private IEnumerable<ICommunicationPlugin> PluginInstances { get; set; }

        public event CommunicationServiceRegistered CommunicationServiceRegisteredEvent;
        public Action<string> LogAction { get; set; }

        public PluginManager()
        {
            LogAction?.Invoke($"{nameof(PluginManager)} initialized.");
        }

        public void LoadPlugins(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                return;
            }

            LogAction?.Invoke($"[{nameof(PluginManager)}] Loading plugins from directory [{Path.GetFullPath(directory)}]");

            var files = Directory.GetFiles(Path.GetFullPath(directory), "*.dll");

            foreach(string file in files)
            {
                if (!File.Exists(file)) continue;

                LogAction?.Invoke($"[{nameof(PluginManager)}] Loading [{file}]");

                PluginLoadContext loadContext = new PluginLoadContext(file);
                LoadedPlugins.Add(loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(file))));
            }

            
        }

        private void CreateInstances()
        {
            if (PluginInstances == null)
            {
                foreach (var plugin in LoadedPlugins)
                {
                    LogAction?.Invoke($"[{nameof(PluginManager)}] Creating instance(s) of plugin [{plugin.FullName}]");
                    if (PluginInstances == null)
                    {
                        PluginInstances = CreateCommands(plugin);
                        continue;
                    }

                    PluginInstances.Concat(CreateCommands(plugin));
                }
            }
        }

        public void ExecutePlugins()
        {
            CreateInstances();

            if (PluginInstances == null) return;

            foreach(var plugin in PluginInstances)
            {
                var ps = new PacketSerializer();
                plugin.Register(ps);
                CommunicationServiceRegisteredEvent?.Invoke(new CommunicationServiceRegisteredArgs {
                    ServiceIdentification = plugin.GameIdentification,
                    PacketSerializer = ps
                });
            }
        }

        static IEnumerable<ICommunicationPlugin> CreateCommands(Assembly assembly)
        {
            int count = 0;

            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(ICommunicationPlugin).IsAssignableFrom(type))
                {
                    ICommunicationPlugin result = Activator.CreateInstance(type) as ICommunicationPlugin;
                    if (result != null)
                    {
                        count++;
                        yield return result;
                    }
                }
            }

            if (count == 0)
            {
                string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
                throw new ApplicationException(
                    $"Can't find any type which implements {nameof(ICommunicationPlugin)} in {assembly} from {assembly.Location}.\n" +
                    $"Available types: {availableTypes}");
            }
        }

        internal class PluginLoadContext : AssemblyLoadContext
        {
            private AssemblyDependencyResolver _resolver;

            public PluginLoadContext(string pluginPath)
            {
                _resolver = new AssemblyDependencyResolver(pluginPath);
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
                if (assemblyPath != null)
                {
                    return LoadFromAssemblyPath(assemblyPath);
                }

                return null;
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
                if (libraryPath != null)
                {
                    return LoadUnmanagedDllFromPath(libraryPath);
                }

                return IntPtr.Zero;
            }
        }

    }
}
