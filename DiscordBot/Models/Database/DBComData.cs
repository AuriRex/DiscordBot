using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models.Database
{
    public class DBComData : DBBaseObject
    {
        public string Hostname { get; set; }
        public int Port { get; set; }
    }
}
