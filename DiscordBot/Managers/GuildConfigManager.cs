using DiscordBot.Attributes;
using DiscordBot.Models.Configuration;
using DSharpPlus.Entities;
using Serilog;
using System.Collections.Generic;
using System.IO;

namespace DiscordBot.Managers
{
    [AutoDI.Singleton]
    public class GuildConfigManager
    {
        private Config _config;
        private Dictionary<ulong, GuildConfig> _guildConfigs;

        public static string GuildConfigFolderPath { get; set; } = "./GuildConfigs/";

        public GuildConfigManager(Config config)
        {
            _config = config;
            _guildConfigs = new Dictionary<ulong, GuildConfig>();
            string path = Path.GetFullPath(GuildConfigFolderPath);
            if(!Directory.Exists(path))
            {
                Log.Logger.Information($"Created GuildConfig directory at [{path}]");
                Directory.CreateDirectory(path);
            }
            LoadGuildConfigs();
        }

        private void LoadGuildConfigs()
        {
            Log.Logger.Information($"Loading Guild Configs ...");
            // TODO
        }

        public GuildConfig GetConfigForGuild(DiscordGuild guild)
        {
            if(_guildConfigs.TryGetValue(guild.Id, out GuildConfig config)) {
                return config;
            }
            return null;
        }

    }
}
