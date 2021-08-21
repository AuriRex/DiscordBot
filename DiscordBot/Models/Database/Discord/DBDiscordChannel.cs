using DiscordBot.Interfaces;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Models.Database.Discord
{
    public class DBDiscordChannel : DBDiscordBaseObject, IDBStorable<DiscordChannel, DBDiscordChannel>, IDBRetrievable<DBDiscordChannel, DiscordChannel>
    {
        public string ChannelName { get; set; }
        public ulong GuildId { get; set; }

        DBDiscordChannel IDBStorable<DiscordChannel, DBDiscordChannel>.FromDiscordObject(DiscordChannel discordChannel)
        {
            DiscordUniqueId = discordChannel.Id;
            ChannelName = discordChannel.Name;
            GuildId = discordChannel.Guild.Id;

            return this;
        }

        async Task<DiscordChannel> IDBRetrievable<DBDiscordChannel, DiscordChannel>.ToDiscordObject(DBDiscordChannel dbChannel, DiscordClient client)
        {
            return await client.GetChannelAsync(dbChannel.DiscordUniqueId);
        }
    }
}