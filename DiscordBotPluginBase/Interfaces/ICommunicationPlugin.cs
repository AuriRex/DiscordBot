using Communicator.Net;

namespace DiscordBotPluginBase.Interfaces
{
    public interface ICommunicationPlugin
    {
        string GameIdentification { get; }
        string Description { get; }

        void Register(PacketSerializer packetSerializer);
    }
}
