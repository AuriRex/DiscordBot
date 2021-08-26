using Communicator.Interfaces;
using DiscordBot.Models.Database;
using System;
using System.Linq;

namespace DiscordBot.Managers
{
    public class ComAuthService : IAuthentificationService
    {
        private DataBaseManager _dbManager;
        public const string kServerAuthCollection = "communicator_service_auth_info";

        public Action<string> LogAction { get; set; }

        public ComAuthService(DataBaseManager dbManager)
        {
            _dbManager = dbManager;
        }

        public bool AuthenticateGameserver(string passwordHash, string serverID, string serviceIdentification)
        {
            if (string.IsNullOrEmpty(passwordHash)) return false;
            if (string.IsNullOrEmpty(serverID)) return false;
            if (string.IsNullOrEmpty(serviceIdentification)) return false;


            var allRegistered = _dbManager.GetAllInCollection<DBCommunicatorGameserverInformation>(kServerAuthCollection);

            if(!allRegistered.Any(x => x.ServerID == serverID && x.ServiceIdentification == serviceIdentification))
            {
                // No server registered, do not auth client.
                LogAction?.Invoke($"Not authenticating client '{serverID}'-'{serviceIdentification}', no matching server in DB!");
                return false;
            }

            var comInfo = allRegistered.First(x => x.ServerID == serverID && x.ServiceIdentification == serviceIdentification);

            if(string.IsNullOrEmpty(comInfo.PasswordHash))
            {
                // Setup mode

                comInfo.PasswordHash = passwordHash;

                _dbManager.InsertOrUpdate(comInfo, kServerAuthCollection);
                LogAction?.Invoke($"Authenticated client '{serverID}'-'{serviceIdentification}' in setup mode, password hash has been saved!");
                return true;
            }

            if(comInfo.ConnectedGuildId == 0L || comInfo.ConnectedChannelId == 0L)
            {
                LogAction?.Invoke($"Not authenticating client '{serverID}'-'{serviceIdentification}' because no Guild and/or Channel has been set!");
                return false;
            }

            if(comInfo.PasswordHash == passwordHash)
            {
                LogAction?.Invoke($"Authenticated client '{serverID}'-'{serviceIdentification}'.");
                return true;
            }

            LogAction?.Invoke($"Not authenticating client '{serverID}'-'{serviceIdentification}', password hash didn't match!");
            return false;
        }

        public bool RegisterServer(string serverId, string serviceIdentification, ulong guildId, ulong channelId)
        {
            var allRegistered = _dbManager.GetAllInCollection<DBCommunicatorGameserverInformation>(kServerAuthCollection);

            if (allRegistered.Any(x => x.ServerID == serverId && x.ServiceIdentification == serviceIdentification))
            {
                return false;
            }

            _dbManager.InsertOrUpdate(new DBCommunicatorGameserverInformation() {
                ConnectedChannelId = channelId,
                ConnectedGuildId = guildId,
                ServerID = serverId,
                ServiceIdentification = serviceIdentification,
            }, kServerAuthCollection);

            return true;
        }

        public (ulong guildId, ulong channelId) GetGuildAndChannelIdsFor(string serverId, string serviceIdentification)
        {
            var allRegistered = _dbManager.GetAllInCollection<DBCommunicatorGameserverInformation>(kServerAuthCollection);

            if (allRegistered.Any(x => x.ServerID == serverId && x.ServiceIdentification == serviceIdentification))
            {
                var comInfo = allRegistered.First(x => x.ServerID == serverId && x.ServiceIdentification == serviceIdentification);

                return (comInfo.ConnectedGuildId, comInfo.ConnectedChannelId);
            }

            throw new ArgumentException($"No auth data for '{serverId}'-'{serviceIdentification}'");
        }

        public (string serverId, string serviceId) GetServerInfoFor(ulong channelId)
        {
            var allRegistered = _dbManager.GetAllInCollection<DBCommunicatorGameserverInformation>(kServerAuthCollection);

            if (allRegistered.Any(x => x.ConnectedChannelId == channelId))
            {
                var comInfo = allRegistered.First(x => x.ConnectedChannelId == channelId);

                

                return (comInfo.ServerID, comInfo.ServiceIdentification);
            }

            throw new ArgumentException($"No auth data for channel with id '{channelId}'!");
        }
    }
}
