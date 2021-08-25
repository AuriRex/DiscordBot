using Communicator.Net;
using DiscordBotPluginBase.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Events
{
    public class CommunicationServiceRegisteredArgs
    {
        public string ServiceIdentification { get; set; }
        public PacketSerializer PacketSerializer { get; set; }
        public ICommunicationPlugin Plugin { get; set; }
    }
}
