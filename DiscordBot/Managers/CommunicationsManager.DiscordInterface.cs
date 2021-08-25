using DiscordBotPluginBase.Interfaces;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Managers
{
    public partial class CommunicationsManager
    {

        public class DiscordInterface : IDiscordInterface
        {

            public DiscordInterface(DiscordClient client, DiscordGuild guild, DiscordChannel channel)
            {

            }

            public void SendEmbedMessage(string message, int r, int g, int b, string authorUrl = null, string authorText = null, string authorLink = null)
            {

            }

            public void SendMessage(string message)
            {

            }
        }

    }
}
