using Communicator.Net;
using DiscordBot.Events;
using DiscordBotPluginBase.Interfaces;

namespace DiscordBot.Models
{
    public class ComServiceContainer
    {
        public string ServiceId { get; private set; }
        public PacketSerializer PacketSerializer { get; private set; }
        public ICommunicationPlugin Plugin { get; private set; }
        public ComServiceContainer(string serviceId, PacketSerializer packetSerializer, ICommunicationPlugin plugin)
        {
            ServiceId = serviceId;
            PacketSerializer = packetSerializer;
            Plugin = plugin;
        }

        public ComServiceContainer(CommunicationServiceRegisteredArgs args)
        {
            ServiceId = args.ServiceIdentification;
            PacketSerializer = args.PacketSerializer;
            Plugin = args.Plugin;
        }

        public ComService CreateNewInstance(string serverId)
        {
            return ComService.Create(serverId, this);
        }
    }
}
