using DiscordBot.Extensions;
using DiscordBot.Interfaces;
using DiscordBot.Models.Database.Discord;
using DSharpPlus;
using DSharpPlus.Entities;
using LiteDB;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot.Managers
{
    public class DataBaseManager
    {
        [Obsolete]
        public const string DISCORDUSER_COLLECTION = "discord_users";
        [Obsolete]
        public const string DISCORDMESSAGE_COLLECTION_PREFIX = "discord_messages_";
        private string _fileDataBasePath;

        public DataBaseManager(string fileDBPath)
        {
            _fileDataBasePath = fileDBPath;
            Log.Logger.Information($"{nameof(DataBaseManager)} initialized. [{Path.GetFullPath(fileDBPath)}]");
        }

        /// <summary>
        /// Insert or update a record of type <typeparamref name="TDB"/>.
        /// </summary>
        /// <typeparam name="TDB"></typeparam>
        /// <param name="obj"></param>
        /// <param name="collection"></param>
        public void InsertOrUpdate<TDB>(TDB obj, string collection)
        {
            // Open database (or create if doesn't exist)
            using (var db = new LiteDatabase(_fileDataBasePath))
            {
                var col = db.GetCollection<TDB>(collection);

                if(!col.Update(obj))
                    col.Insert(obj);   
            }
        }

        /// <summary>
        /// Gets the first found item <typeparamref name="TDB"/> from the collection <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="TDB"></typeparam>
        /// <param name="collection"></param>
        /// <returns>The first item <typeparamref name="TDB"/> or <paramref name="null"/> if none is found.</returns>
        public TDB GetFirstFromCollection<TDB>(string collection) where TDB : class
        {
            using (var db = new LiteDatabase(_fileDataBasePath))
            {
                var col = db.GetCollection<TDB>(collection);

                var all = new List<TDB>(col.FindAll());

                if(all.Count > 0)
                    return all[0];

                return null;
            }
        }

        /// <summary>
        /// Get all items <typeparamref name="TDB"/> from the collection <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="TDB"></typeparam>
        /// <param name="collection"></param>
        /// <returns>A populated <see cref="List{TDB}"/> or an empty one if no items of type <typeparamref name="TDB"/> are found.</returns>
        public List<TDB> GetAllInCollection<TDB>(string collection)
        {
            using (var db = new LiteDatabase(_fileDataBasePath))
            {
                var col = db.GetCollection<TDB>(collection);

                var all = col.FindAll();

                if (all == null) return new List<TDB>();

                return new List<TDB>(all);
            }
        }

        public void InsertOrUpdateDiscordObject<T, TDB>(T discordObj, string collection) where TDB : DBDiscordBaseObject, IDBStorable<T, TDB>, new() where T : SnowflakeObject
        {
            using (var db = new LiteDatabase(_fileDataBasePath))
            {
                var col = db.GetCollection<TDB>(collection);

                var dbObj = col.FindOne(x => x.DiscordUniqueId == discordObj.Id);
                if (dbObj != null)
                {
                    dbObj.CloneFrom(IDBStorable<T,TDB>.FromDiscord(discordObj));
                    col.Update(dbObj);
                    Log.Logger.Warning($"Updated object '{discordObj.Id}' in DB!");
                    return;
                }

                col.Insert(IDBStorable<T, TDB>.FromDiscord(discordObj));
                Log.Logger.Information($"Inserted object '{discordObj.Id}' into DB!");
            }
        }

        public async Task<T> GetDiscordObject<TDB, T>(ulong id, string collection, DiscordClient client) where TDB : DBDiscordBaseObject, IDBRetrievable<TDB, T>, new() where T : SnowflakeObject
        {
            using (var db = new LiteDatabase(_fileDataBasePath))
            {
                var col = db.GetCollection<TDB>(collection);

                var dbObj = col.FindOne(x => x.DiscordUniqueId == id);
                if (dbObj != null)
                {
                    return await IDBRetrievable<TDB, T>.ToDiscord(dbObj, client);
                    //Log.Logger.Warning($"Updated user '{discordObj.Id}' in DB!");
                }
            }
            return null;
        }
    }
}
