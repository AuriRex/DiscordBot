using Communicator.Net;
using Communicator.Packets;
using DiscordBot.Events;
using DiscordBot.Models;
using DiscordBot.Models.Database;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using static DiscordBot.Managers.CommunicationsManager.MyCoolCustomEventPacket;

namespace DiscordBot.Managers
{
    [Attributes.AutoDI.Singleton]
    public partial class CommunicationsManager
    {
        public ComAuthService CustomAuthService { private get; set; }
        public DataBaseManager DBManager { private get; set; }

        public const string kComData = "communication_manager_data";

        private static Server CommunicatorServer { get; set; }

        public class MyCoolCustomEventPacket : BasePacket<CustomEventData>
        {
            public override CustomEventData PacketData { get; set; }

            public class CustomEventData
            {
                public string Message { get; set; }
            }
        }

        private Dictionary<string, ComServiceContainer> RegisteredCommunicationSevices = new Dictionary<string, ComServiceContainer>();

        public CommunicationsManager()
        {
            
        }

        public void Initialize()
        {
            CustomAuthService.LogAction = Log.Logger.Information;
            CommunicatorServer = new Server();
            CommunicatorServer.RegisterCustomPacket<MyCoolCustomEventPacket>();
            CommunicatorServer.LogAction = (s) => { Log.Logger.Information($"[CommunicatorServer] {s}"); };
            CommunicatorServer.ErrorLogAction = (s) => { Log.Logger.Error($"[CommunicatorServer] {s}"); };
            CommunicatorServer.ClientConnectedEvent += CommunicatorServer_ClientConnectedEvent;
            CommunicatorServer.AuthentificationService = CustomAuthService;
        }

        public bool RegisterServer(string serverId, string serviceIdentification, ulong guildId, ulong channelId)
        {
            return CustomAuthService.RegisterServer(serverId, serviceIdentification, guildId, channelId);
        }

        public (string hostname, int port) GetHostInfo()
        {
            var comData = DBManager.GetFirstFromCollection<DBComData>(kComData);

            if (comData == null) return ("Unset!!", 0);

            return (comData.Hostname, comData.Port);
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

        public List<string> ListAllRegisteredServices()
        {
            return new List<string>(RegisteredCommunicationSevices.Keys);
        }

        internal void SetHostname(string hostname, int port)
        {
            var comData = DBManager.GetFirstFromCollection<DBComData>(kComData);

            comData = comData ?? new DBComData();

            comData.Hostname = hostname;
            comData.Port = port;

            DBManager.InsertOrUpdate(comData, kComData);
        }

        internal void RegisterService(CommunicationServiceRegisteredArgs args)
        {
            if (RegisteredCommunicationSevices.ContainsKey(args.ServiceIdentification)) throw new ArgumentException($"Tried to add duplicate service with id '{args.ServiceIdentification}'!");
            Log.Logger.Information($"[{nameof(CommunicationsManager)}] ServiceId '{args.ServiceIdentification}' has been registered!");
            RegisteredCommunicationSevices.Add(args.ServiceIdentification, new ComServiceContainer(args));
        }
    }
}
