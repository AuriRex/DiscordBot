using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models
{
    public class ComService : ComServiceContainer
    {
        public string ServerId { get; set; }

        private ComService(string serverId, ComServiceContainer container) : base(container.ServiceId, container.PacketSerializer, container.Plugin)
        {
            ServerId = serverId;
        }

        internal static ComService Create(string serverId, ComServiceContainer container)
        {
            return new ComService(serverId, container);
        }

    }
}
