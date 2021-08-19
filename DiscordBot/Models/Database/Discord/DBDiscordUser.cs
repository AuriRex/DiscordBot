using DiscordBot.Interfaces;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Models.Database.Discord
{
    public class DBDiscordUser : DBDiscordBaseObject, IDBStorable<DiscordUser, DBDiscordUser>, IDBRetrievable<DBDiscordUser, DiscordUser>
    {
        public string Username { get; set; }
        public string Discriminator { get; set; }
        public string AvatarUrl { get; set; }

        DBDiscordUser IDBStorable<DiscordUser, DBDiscordUser>.FromDiscordObject(DiscordUser discordUser)
        {
            DiscordUniqueId = discordUser.Id;
            Username = discordUser.Username;
            Discriminator = discordUser.Discriminator;
            AvatarUrl = discordUser.AvatarUrl;

            return this;
        }

        async Task<DiscordUser> IDBRetrievable<DBDiscordUser, DiscordUser>.ToDiscordObject(DBDiscordUser dbUser, DiscordClient client)
        {
            return await client.GetUserAsync(dbUser.DiscordUniqueId);
        }
    }
}
