using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models.Database.Discord
{
    public class DBDiscordBaseObject : DBBaseObject
    {
        public ulong DiscordUniqueId { get; set; }
    }
}
