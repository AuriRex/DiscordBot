using DiscordBot.Managers;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using DiscordBot.Models.Configuration;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using static DiscordBot.Utilities;
using DiscordBot.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using Communicator.Net;
using Communicator.Packets;

namespace DiscordBot
{
    class Program
    {
        public const string DEFAULT_CONFIG_LOCATION = "./data/config.json";
        public static string ConfigLocation { get; set; } = DEFAULT_CONFIG_LOCATION;

        internal static DiscordClient DiscordClientInstance { get; private set; }
        private static Config ConfigInstance { get; set; }
        private static PluginManager PluginManager { get; set; }

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
                Log.Logger.Information("Disconnecting from discord ...");
                DiscordClientInstance?.DisconnectAsync();
                Log.Logger.Information($"Saving Config ... [{Path.GetFullPath(ConfigLocation)}]");
                SaveJSON(ConfigInstance, ConfigLocation);
#if DEBUG
                Task.Delay(1000).Wait();
#endif
                process.Kill();
            };

            if (!Utilities.QuoteUnitTestsQuote())
            {
                Log.Logger.Fatal("Tests returned false!");
            }

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => { applicationExitAction?.Invoke(sender, eventArgs); };
            process.Exited += (sender, eventArgs) => { applicationExitAction?.Invoke(sender, eventArgs); };
            Console.CancelKeyPress += (sender, eventArgs) => { applicationExitAction?.Invoke(sender, eventArgs); };

            Log.Logger.Information($"Loading Config ... [{Path.GetFullPath(ConfigLocation)}]");
            Config cfg;
            if (!TryLoadJSON(ConfigLocation, out cfg))
            {
                Log.Logger.Information("Config doesn't exist, saving default config values ...");
                SaveJSON(cfg, ConfigLocation);
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

            var services = new ServiceCollection();

            var DBM = new DataBaseManager("./data/database.db");

            

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
                    }
                }
            }

            services.AddSingleton<Config>(ConfigInstance);
            services.AddSingleton<DataBaseManager>(DBM);
            services.AddSingleton<Random>();


            var cmdConf = new CommandsNextConfiguration();

            if (ConfigInstance.UseTextPrefix)
                cmdConf.StringPrefixes = ConfigInstance.Prefixes;
            var serviceProvider = services.BuildServiceProvider();
            cmdConf.Services = serviceProvider;


            var commands = discord.UseCommandsNext(cmdConf);

            commands.RegisterCommands(Assembly.GetExecutingAssembly());

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

            await discord.ConnectAsync();
            Program.DiscordClientInstance = discord;

            var comManager = serviceProvider.GetService<CommunicationsManager>();


            PluginManager.CommunicationServiceRegisteredEvent += comManager.RegisterService;

            PluginManager.ExecutePlugins();


            await Task.Delay(-1);
        }

        private static Task Commands_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e) => throw new NotImplementedException();
    }
}
