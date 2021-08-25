using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBotPluginBase.Interfaces
{
    public interface IDiscordInterface
    {

        public void SendMessage(string message);
        public void SendEmbedMessage(string message, int r, int g, int b, string authorUrl = null, string authorText = null, string authorLink = null);

    }
}
