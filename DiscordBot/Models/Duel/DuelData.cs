using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DiscordBot.Models.Duel
{
    public class DuelData
    {
        [JsonPropertyName("O")]
        public ulong UserOne { get; set; }
        [JsonPropertyName("T")]
        public ulong UserTwo { get; set; }
        [JsonPropertyName("B")]
        public int BestOf { get; set; }
        [JsonPropertyName("C")]
        public int Current { get; set; }

    }
}
