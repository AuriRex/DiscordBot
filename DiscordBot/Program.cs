using DiscordBot.Attributes;
using DiscordBot.Commands.Application;
using DiscordBot.Managers;
using DiscordBot.Models.Configuration;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static DiscordBot.Utilities;

[assembly: AssemblyFileVersion(
    ThisAssembly.Git.SemVer.Major + "." +
    ThisAssembly.Git.SemVer.Minor + "." +
    ThisAssembly.Git.Commits + ".0")]
[assembly: AssemblyInformationalVersion(
    ThisAssembly.Git.SemVer.Major + "." +
    ThisAssembly.Git.SemVer.Minor + "." +
    ThisAssembly.Git.Commits + "-" +
    ThisAssembly.Git.Branch + "+" +
    ThisAssembly.Git.Commit)]
[assembly: AssemblyVersion(
    ThisAssembly.Git.SemVer.Major + "." +
    ThisAssembly.Git.SemVer.Minor + "." +
    ThisAssembly.Git.Commits + ".0")]

namespace DiscordBot
{
    class Program
    {
        public const string DEFAULT_CONFIG_LOCATION = "./data/config.json";
        public static string ConfigLocation { get; set; } = DEFAULT_CONFIG_LOCATION;

        internal static DiscordClient DiscordClientInstance { get; private set; }
        private static Config ConfigInstance { get; set; }
        private static PluginManager PluginManager { get; set; }
        private static CommunicationsManager ComManager { get; set; } = null;
        static void Main(string[] args)
        {
            Init();

            LoadPlugins();

            MainAsync().GetAwaiter().GetResult();
        }

        private static void LoadPlugins()
        {
            PluginManager.LogAction = Log.Logger.Information;
            PluginManager.LoadPlugins("./data/plugins/");
        }

        private static void Init()
        {
            var process = Process.GetCurrentProcess();
            process.EnableRaisingEvents = true;

            PluginManager = new PluginManager();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Action<object, EventArgs> applicationExitAction = (sender, eventArgs) => {
                Log.Logger.Information("Application closing ...");
                ComManager?.OnApplicationClosing();
                Log.Logger.Information("Disconnecting from discord ...");
                DiscordClientInstance?.DisconnectAsync();
                Log.Logger.Information($"Saving Config ... [{Path.GetFullPath(ConfigLocation)}]");
                SaveJSONToFile(ConfigInstance, ConfigLocation);
#if DEBUG
                Task.Delay(1000).Wait();
#endif

                process.Kill();
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => { applicationExitAction?.Invoke(sender, eventArgs); };
            process.Exited += (sender, eventArgs) => { applicationExitAction?.Invoke(sender, eventArgs); };
            Console.CancelKeyPress += (sender, eventArgs) => { applicationExitAction?.Invoke(sender, eventArgs); };

            Log.Logger.Information($"Loading Config ... [{Path.GetFullPath(ConfigLocation)}]");
            Config cfg;
            if (!TryLoadJSONFromFile(ConfigLocation, out cfg))
            {
                Log.Logger.Information("Config doesn't exist, saving default config values ...");
                SaveJSONToFile(cfg, ConfigLocation);
            }
            ConfigInstance = cfg;
        }

        static async Task MainAsync()
        {
            string token = Environment.GetEnvironmentVariable("bot_token");

            if (string.IsNullOrEmpty(token))
            {
                Log.Logger.Error("No token has been set! Make sure your bot token is set in the environment variable 'bot_token'!");
                return;
            }

            var logFactory = new LoggerFactory().AddSerilog();

            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged,
                LoggerFactory = logFactory
            });

            var lavalinkEndpoint = new ConnectionEndpoint
            {
                Hostname = ConfigInstance.LavaLink.Hostname, // From your server configuration.
                Port = ConfigInstance.LavaLink.Port // From your server configuration
            };

            var lavalinkAuth = Environment.GetEnvironmentVariable("lavalink_auth");
            
            if(string.IsNullOrEmpty(lavalinkAuth))
            {
                Log.Warning($"Environment variable 'lavalink_auth' is empty, is this intentional?");
            }

            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = lavalinkAuth, // From your server configuration.
                RestEndpoint = lavalinkEndpoint,
                SocketEndpoint = lavalinkEndpoint
            };

            var services = new ServiceCollection();

            var DBM = new DataBaseManager("./data/database.db");
            var authService = new ComAuthService(DBM);
            ComManager = new CommunicationsManager(authService, DBM, discord);

            List<object> toInstall = new List<object>();

            foreach(Type t in Assembly.GetExecutingAssembly().GetTypes())
            {
                if(t.IsDefined(typeof(AutoDI.DiscordBotDIA)))
                {
                    switch(t.GetCustomAttribute<AutoDI.DiscordBotDIA>())
                    {
                        case AutoDI.Singleton attr:
                            services.AddSingleton(t);
                            break;
                        case AutoDI.Transient attr:
                            services.AddTransient(t);
                            break;
                        case AutoDI.Scoped attr:
                            services.AddScoped(t);
                            break;
                        case AutoDI.SingletonCreateAndInstall attr:
                            var instance = Activator.CreateInstance(t);
                            services.AddSingleton(t, instance);
                            toInstall.Add(instance);
                            break;
                    }
                }
            }

            services.AddSingleton<Config>(ConfigInstance);
            services.AddSingleton<DataBaseManager>(DBM);
            services.AddSingleton<CommunicationsManager>(ComManager);
            services.AddSingleton<ComAuthService>(authService);
            services.AddSingleton<Random>();

            var serviceProvider = services.BuildServiceProvider();

            foreach (var instance in toInstall)
            {
                var type = instance.GetType();
                var props = type.GetRuntimeProperties().Where(xp => xp.CanWrite && xp.SetMethod != null && !xp.SetMethod.IsStatic && xp.SetMethod.IsPublic);
                foreach (var prop in props)
                {
                    if (prop.GetCustomAttribute<DontInjectAttribute>() != null)
                        continue;

                    var service = serviceProvider.GetService(prop.PropertyType);
                    if (service == null)
                        continue;

                    prop.SetValue(instance, service);
                }
            }

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration {
                StringPrefixes = ConfigInstance.UseTextPrefix ? ConfigInstance.Prefixes : null,
                Services = serviceProvider
            });

            var slash = discord.UseSlashCommands(new SlashCommandsConfiguration {
                Services = serviceProvider
            });

            commands.RegisterCommands(Assembly.GetExecutingAssembly());

            slash.RegisterCommands(Assembly.GetExecutingAssembly(), 871002087692058625);

            commands.CommandExecuted += (sender, e) => {
                string where = e.Context.Channel.IsPrivate ? $"DM Channel with id:'{e.Context.Channel?.Id}'" : $"'{e.Context.Guild?.Name}'-'{e.Context.Channel?.Name}' Ids: '{e.Context.Guild?.Id}'-'{e.Context.Channel?.Id}'";
                Log.Logger.Information($"{e.Context.User?.Username ?? "Someone"}#{e.Context.User?.Discriminator} used '{e?.Command?.Name}' in {where}");

                return Task.CompletedTask;
            };

            commands.CommandErrored += (sender, e) => {
                if (e.Exception.GetType().IsAssignableFrom(typeof(CommandNotFoundException)))
                {
                    Log.Logger.Information($"{e.Context.User?.Username ?? "Someone"}#{e.Context.User?.Discriminator} tried to use non-existent command with message '{e.Context.Message?.Content}'");
                    return Task.CompletedTask;
                }
                string where = e.Context.Channel.IsPrivate ? $"DM Channel with id:'{e.Context.Channel?.Id}'" : $"'{e.Context.Guild?.Name}'-'{e.Context.Channel?.Name}' Ids: '{e.Context.Guild?.Id}'-'{e.Context.Channel?.Id}'";
                Log.Logger.Error($"{e.Context.User?.Username ?? "Someone"}#{e.Context.User?.Discriminator} used '{e?.Command?.Name}' in {where} Exception: {e?.Exception}");

                return Task.CompletedTask;
            };

            ComManager.Initialize();

            PluginManager.CommunicationServiceRegisteredEvent += ComManager.RegisterService;

            PluginManager.ExecutePlugins();

            discord.MessageCreated += ComManager.MessageCreated;
            discord.ComponentInteractionCreated += InteractionHandler.ComponentInteractionCreated;

            var lavalink = discord.UseLavalink();

            await discord.ConnectAsync();

            discord.Ready += async (s, e) => {

                await discord.UpdateStatusAsync(new DiscordActivity($"the last {ThisAssembly.Git.Commits} Commits", ActivityType.Watching));

            };

            Program.DiscordClientInstance = discord;
            try
            {
                await lavalink.ConnectAsync(lavalinkConfig);
            }
            catch(Exception ex)
            {
                Log.Error($"An exception occured while connecting to LavaLink: {ex.Message}");
                Log.Error($"{ex.StackTrace}");
            }

            await Task.Delay(-1);
        }
    }
}
