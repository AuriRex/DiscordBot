using Communicator.Interfaces;
using Communicator.Net;

namespace DiscordBotPluginBase.Interfaces
{
    public interface ICommunicationPlugin
    {
        IDiscordInterface DiscordInterface { get; set; }
        string GameIdentification { get; }
        string Description { get; }

        void Register(PacketSerializer packetSerializer);

        void OnPacketReceived(IPacket packet);

        void OnDiscordMessageReceived(string message, string username);
    }
}
