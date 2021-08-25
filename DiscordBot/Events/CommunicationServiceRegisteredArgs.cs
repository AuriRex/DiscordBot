using Communicator.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Events
{
    public class CommunicationServiceRegisteredArgs
    {
        public string ServiceIdentification { get; set; }
        public PacketSerializer PacketSerializer { get; set; }
    }
}
