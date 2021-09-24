using Communicator.Net;
using Communicator.Packets;
using DiscordBot.Events;
using DiscordBot.Models;
using DiscordBot.Models.Database;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DiscordBot.Managers.CommunicationsManager.MyCoolCustomEventPacket;

namespace DiscordBot.Managers
{
    public partial class CommunicationsManager
    {
        private ComAuthService _authService;
        private DataBaseManager _dbManager;
        private DiscordClient _discordClient;

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

        private Dictionary<string, ComServiceTemplate> _registeredCommunicationSevices = new Dictionary<string, ComServiceTemplate>();
        private Dictionary<string, Client> _clientServerIDDictionary = new Dictionary<string, Client>();
        private Dictionary<Client, ComService> _clientServiceDictionary = new Dictionary<Client, ComService>();

        public CommunicationsManager(ComAuthService authService, DataBaseManager dbManager, DiscordClient discordClient)
        {
            _authService = authService;
            _dbManager = dbManager;
            _discordClient = discordClient;
        }

        public void Initialize()
        {
            _authService.LogAction = Log.Logger.Information;
            CommunicatorServer = new Server();
            CommunicatorServer.RegisterCustomPacket<MyCoolCustomEventPacket>();
            CommunicatorServer.LogAction = (s) => { Log.Logger.Information($"[CommunicatorServer] {s}"); };
            CommunicatorServer.ErrorLogAction = (s) => { Log.Logger.Error($"[CommunicatorServer] {s}"); };
            CommunicatorServer.ClientConnectedEvent += CommunicatorServer_ClientConnectedEvent;
            CommunicatorServer.AuthentificationService = _authService;
        }

        internal void OnApplicationClosing()
        {
            CommunicatorServer?.WaitStopServer();
        }

        public bool RegisterServer(string serverId, string serviceIdentification, ulong guildId, ulong channelId)
        {
            return _authService.RegisterServer(serverId, serviceIdentification, guildId, channelId);
        }

        public (string hostname, int port) GetHostInfo()
        {
            var comData = _dbManager.GetFirstFromCollection<DBComData>(kComData);

            if (comData == null) return ("Unset!!", 0);

            return (comData.Hostname, comData.Port);
        }

        private void CommunicatorServer_ClientConnectedEvent(Communicator.Net.EventArgs.ClientConnectedEventArgs e)
        {
            Log.Logger.Information($"A Client has connected: {e.ServerID} Game:{e.ServiceName}");

            // e.Client.PacketSerializer

            if (_registeredCommunicationSevices.TryGetValue(e.ServiceName, out ComServiceTemplate template)) {
                template.Plugin.Register(e.Client.PacketSerializer);
                var serviceContainer = template.CreateNewInstance(e.ServerID);

                var gci = _authService.GetGuildAndChannelIdsFor(e.ServerID, e.ServiceName);

                serviceContainer.Plugin.DiscordInterface = new DiscordInterface(_discordClient, gci.guildId, gci.channelId);
                serviceContainer.Plugin.Client = e.Client;

                _clientServiceDictionary.Add(e.Client, serviceContainer);
                _clientServerIDDictionary.Add(serviceContainer.ServerId, e.Client);
            }

            e.Client.DisconnectedEvent += Client_DisconnectedEvent;
            e.Client.PacketReceivedEvent += Client_PacketReceivedEvent;
        }

        private void Client_PacketReceivedEvent(object sender, Communicator.Interfaces.IPacket e)
        {
            Client client = (Client) sender;

            Log.Logger.Information($"Received packet {e.GetType()} -> {(e.GetType() == typeof(MyCoolCustomEventPacket) ? ((MyCoolCustomEventPacket) e).PacketData.Message : $"{e.EventTime}")}");

            if (_clientServiceDictionary.TryGetValue(client, out ComService serviceContainer))
            {
                serviceContainer.Plugin.OnPacketReceived(e);
            }
        }

        private void Client_DisconnectedEvent(Communicator.Net.EventArgs.ClientDisconnectedEventArgs e)
        {
            Client client = e.Client;

            Log.Logger.Information($"A client has disconnected.");

            if(_clientServiceDictionary.TryGetValue(client, out ComService serviceContainer))
            {
                _clientServerIDDictionary.Remove(serviceContainer.ServerId);
            }
            _clientServiceDictionary.Remove(client);
            

            client.PacketReceivedEvent -= Client_PacketReceivedEvent;
            client.DisconnectedEvent -= Client_DisconnectedEvent;
        }

        public List<string> ListAllRegisteredServices()
        {
            return new List<string>(_registeredCommunicationSevices.Keys);
        }

        internal void SetHostname(string hostname, int port)
        {
            var comData = _dbManager.GetFirstFromCollection<DBComData>(kComData);

            comData = comData ?? new DBComData();

            comData.Hostname = hostname;
            comData.Port = port;

            _dbManager.InsertOrUpdate(comData, kComData);
        }

        internal void RegisterService(CommunicationServiceRegisteredArgs args)
        {
            if (_registeredCommunicationSevices.ContainsKey(args.ServiceIdentification)) throw new ArgumentException($"Tried to add duplicate service with id '{args.ServiceIdentification}'!");
            Log.Logger.Information($"[{nameof(CommunicationsManager)}] ServiceId '{args.ServiceIdentification}' has been registered!");
            _registeredCommunicationSevices.Add(args.ServiceIdentification, new ComServiceTemplate(args));
        }

        internal Task MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Channel.IsPrivate) return Task.CompletedTask;
            if (e.Author.IsBot) return Task.CompletedTask;

            _ = Task.Run(() => {
                try
                {
                    var info = _authService.GetServerInfoFor(e.Channel.Id);

                    if(_clientServerIDDictionary.TryGetValue(info.serverId, out Client client))
                    {
                        if(_clientServiceDictionary.TryGetValue(client, out ComService serviceContainer))
                        {
                            serviceContainer.Plugin.OnDiscordMessageReceived(e.Message.Content, e.Author.Username, e.Author.Discriminator);
                        }
                    }
                }
                catch(Exception)
                {

                }
            });


            return Task.CompletedTask;
        }
    }
}
