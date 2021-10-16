using DSharpPlus;
using DSharpPlus.Entities;
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

        public ReactionSettings CustomReactionSettings { get; set; } = new ReactionSettings();

        public class ReactionSettings
        {
            public ulong LeaveCommandReactionId { get; set; }
            public ulong JoinCommandReactionId { get; set; }
            public ulong ResumeCommandReactionId { get; set; }
            public ulong PauseCommandReactionId { get; set; }

            
        }

        public static DiscordEmoji GetGuildEmojiOrFallback(DiscordClient client, ulong id, string unicodeFallback)
        {
            if (string.IsNullOrEmpty(unicodeFallback)) unicodeFallback = "👋";
            DiscordEmoji emoji;
            if (id == 0 || !DiscordEmoji.TryFromGuildEmote(client, id, out emoji))
            {
                emoji = DiscordEmoji.FromUnicode(unicodeFallback);
            }
            return emoji;
        }
    }
}
