using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models.Database
{
    public class DBCommunicatorGameserverInformation : DBBaseObject
    {
        public string ServerID { get; set; } = string.Empty;
        public string GameIdentification { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTimeOffset TimeAdded { get; set; } = DateTimeOffset.Now;
        public ulong ConnectedGuildId { get; set; } = 0L;
        public ulong ConnectedChannelId { get; set; } = 0L;
    }
}
