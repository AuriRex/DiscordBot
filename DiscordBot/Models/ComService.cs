using DiscordBotPluginBase.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Models
{
    public class ComService : ComServiceTemplate
    {
        public string ServerId { get; set; }

        private ComService(string serverId, ComServiceTemplate container) : base(container.ServiceId, Activator.CreateInstance(container.Plugin.GetType()) as ICommunicationPlugin)
        {
            ServerId = serverId;
        }

        internal static ComService Create(string serverId, ComServiceTemplate container)
        {
            return new ComService(serverId, container);
        }

    }
}
