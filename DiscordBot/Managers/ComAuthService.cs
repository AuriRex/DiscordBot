using Communicator.Interfaces;
using DiscordBot.Models.Database;
using System;
using System.Linq;

namespace DiscordBot.Managers
{
    [Attributes.AutoDI.Singleton]
    public class ComAuthService : IAuthentificationService
    {
        public DataBaseManager DBManager { private get; set; }
        public const string kServerAuthCollection = "communicator_service_auth_info";

        public Action<string> LogAction { get; set; }

        public ComAuthService()
        {

        }

        public bool AuthenticateGameserver(string passwordHash, string serverID, string serviceIdentification)
        {
            if (string.IsNullOrEmpty(passwordHash)) return false;
            if (string.IsNullOrEmpty(serverID)) return false;
            if (string.IsNullOrEmpty(serviceIdentification)) return false;


            var allRegistered = DBManager.GetAllInCollection<DBCommunicatorGameserverInformation>(kServerAuthCollection);

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

                DBManager.InsertOrUpdate(comInfo, kServerAuthCollection);
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
            var allRegistered = DBManager.GetAllInCollection<DBCommunicatorGameserverInformation>(kServerAuthCollection);

            if (allRegistered.Any(x => x.ServerID == serverId && x.ServiceIdentification == serviceIdentification))
            {
                return false;
            }

            DBManager.InsertOrUpdate(new DBCommunicatorGameserverInformation() {
                ConnectedChannelId = channelId,
                ConnectedGuildId = guildId,
                ServerID = serverId,
                ServiceIdentification = serviceIdentification,
            }, kServerAuthCollection);

            return true;
        }
    }
}
