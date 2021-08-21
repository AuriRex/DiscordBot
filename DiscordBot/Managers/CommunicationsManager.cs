using Communicator.Net;
using Communicator.Packets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using static DiscordBot.Managers.CommunicationsManager.MyCoolCustomEventPacket;

namespace DiscordBot.Managers
{
    [Attributes.AutoDI.Singleton]
    public class CommunicationsManager
    {
        private static Server CommunicatorServer { get; set; }

        public class MyCoolCustomEventPacket : BasePacket<CustomEventData>
        {
            public override CustomEventData PacketData { get; set; }

            public class CustomEventData
            {
                public string Message { get; set; }
            }
        }

        public CommunicationsManager()
        {
            CommunicatorServer = new Server();
            CommunicatorServer.PacketSerializer.RegisterPacket<MyCoolCustomEventPacket>();
            CommunicatorServer.LogAction = (s) => { Log.Logger.Information($"[CommunicatorServer] {s}"); };
            CommunicatorServer.ErrorLogAction = (s) => { Log.Logger.Error($"[CommunicatorServer] {s}"); };
            CommunicatorServer.ClientConnectedEvent += CommunicatorServer_ClientConnectedEvent;
        }

        private static void CommunicatorServer_ClientConnectedEvent(Communicator.Net.EventArgs.ClientConnectedEventArgs e)
        {
            Log.Logger.Information($"A Client has connected: {e.ServerID} Game:{e.GameName}");

            e.Client.DisconnectedEvent += Client_DisconnectedEvent;
            e.Client.PacketReceivedEvent += Client_PacketReceivedEvent;
        }

        private static void Client_PacketReceivedEvent(object sender, Communicator.Interfaces.IPacket e)
        {
            Client client = (Client) sender;

            Log.Logger.Information($"Received packet {e.GetType()} -> {(e.GetType() == typeof(MyCoolCustomEventPacket) ? ((MyCoolCustomEventPacket) e).PacketData.Message : $"{e.EventTime}")}");

        }

        private static void Client_DisconnectedEvent(Communicator.Net.EventArgs.ClientDisconnectedEventArgs e)
        {
            Client client = e.Client;

            Log.Logger.Information($"A client has disconnected.");

            client.PacketReceivedEvent -= Client_PacketReceivedEvent;
            client.DisconnectedEvent -= Client_DisconnectedEvent;
        }

        public List<string> ListAllGames()
        {
            // TODO
            return new List<string>();
        }

        // TODO: set hostname in db that gets displayed to users using the register command
        internal void SetHostname(string hostname)
        {

        }
    }
}
