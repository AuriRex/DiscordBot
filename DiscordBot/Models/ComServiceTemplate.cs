using Communicator.Net;
using DiscordBot.Events;
using DiscordBotPluginBase.Interfaces;

namespace DiscordBot.Models
{
    public class ComServiceTemplate
    {
        public string ServiceId { get; private set; }
        public ICommunicationPlugin Plugin { get; private set; }
        public ComServiceTemplate(string serviceId, ICommunicationPlugin plugin)
        {
            ServiceId = serviceId;
            Plugin = plugin;
        }

        public ComServiceTemplate(CommunicationServiceRegisteredArgs args)
        {
            ServiceId = args.ServiceIdentification;
            Plugin = args.Plugin;
        }

        public ComService CreateNewInstance(string serverId)
        {
            return ComService.Create(serverId, this);
        }
    }
}
