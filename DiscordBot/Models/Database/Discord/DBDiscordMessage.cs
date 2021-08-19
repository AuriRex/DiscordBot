using DiscordBot.Interfaces;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Models.Database.Discord
{
    public class DBDiscordMessage : DBDiscordBaseObject, IDBStorable<DiscordMessage, DBDiscordMessage>
    {
        public ulong AuthorId { get; set; }
        public ulong ChannelId { get; set; }
        public DateTimeOffset CreationTimestamp { get; set; }
        public string Content { get; set; }

        DBDiscordMessage IDBStorable<DiscordMessage, DBDiscordMessage>.FromDiscordObject(DiscordMessage discordMessage)
        {
            DiscordUniqueId = discordMessage.Id;
            AuthorId = discordMessage.Author.Id;
            ChannelId = discordMessage.ChannelId;
            CreationTimestamp = discordMessage.CreationTimestamp;
            Content = discordMessage.Content;

            return this;
        }
    }
}
