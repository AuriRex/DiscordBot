using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models.Configuration
{
    public class Config
    {
        public bool UseTextPrefix { get; set; } = true;
        public List<string> Prefixes { get; set; } = new List<string>() { "!" };
        public string TestString { get; set; } = "This is a test string!";
    }
}
