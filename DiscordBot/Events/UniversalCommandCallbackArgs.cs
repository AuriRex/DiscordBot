using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Events
{
    public class UniversalCommandCallbackArgs
    {

        public string Content { get; set; }
        public DiscordEmbedBuilder Embed { get; set; }

    }
}
