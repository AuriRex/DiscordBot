using DiscordBot.Interfaces;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Models.Database.Discord
{
    public class DBDiscordGuild : DBDiscordBaseObject, IDBStorable<DiscordGuild, DBDiscordGuild>, IDBRetrievable<DBDiscordGuild, DiscordGuild>
    {
        public string GuildName { get; set; }

        DBDiscordGuild IDBStorable<DiscordGuild, DBDiscordGuild>.FromDiscordObject(DiscordGuild discordGuild)
        {
            DiscordUniqueId = discordGuild.Id;
            GuildName = discordGuild.Name;

            return this;
        }

        async Task<DiscordGuild> IDBRetrievable<DBDiscordGuild, DiscordGuild>.ToDiscordObject(DBDiscordGuild dbGuild, DiscordClient client)
        {
            return await client.GetGuildAsync(dbGuild.DiscordUniqueId);
        }
    }
}
