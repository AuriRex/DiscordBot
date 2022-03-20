using DSharpPlus.Entities;
using System.Collections.Generic;

namespace DiscordBot.Managers
{
    public abstract class BaseGuildManager<T> where T : class, new()
    {
        private readonly Dictionary<DiscordGuild, T> _dataForGuild = new Dictionary<DiscordGuild, T>();

        public T GetOrCreateGSData(DiscordGuild guild)
        {
            if (guild == null) return null;

            if (_dataForGuild.TryGetValue(guild, out T value))
            {
                return value;
            }

            var newGuildData = new T();
            _dataForGuild.Add(guild, newGuildData);
            return newGuildData;
        }
    }
}
