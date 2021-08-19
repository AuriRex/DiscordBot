using DSharpPlus;
using System.Threading.Tasks;

namespace DiscordBot.Interfaces
{
    public interface IDBRetrievable<DBT, T> where DBT : IDBRetrievable<DBT, T>
    {
        protected Task<T> ToDiscordObject(DBT obj, DiscordClient client);
        public static async Task<T> ToDiscord(DBT obj, DiscordClient client)
        {
            return await default(DBT).ToDiscordObject(obj, client);
        }
    }
}
