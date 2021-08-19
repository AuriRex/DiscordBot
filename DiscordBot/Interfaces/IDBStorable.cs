namespace DiscordBot.Interfaces
{
    public interface IDBStorable<T, DBT> where DBT : IDBStorable<T, DBT>, new()
    {
        protected DBT FromDiscordObject(T obj);
        public static DBT FromDiscord(T obj)
        {
            return new DBT().FromDiscordObject(obj);
        }
    }
}
