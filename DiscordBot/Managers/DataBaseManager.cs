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
        public const string DISCORDUSER_COLLECTION = "discord_users";
        public const string DISCORDMESSAGE_COLLECTION_PREFIX = "discord_messages_";
        private string _fileDataBasePath;

        public DataBaseManager(string fileDBPath)
        {
            _fileDataBasePath = fileDBPath;
            Log.Logger.Information($"{nameof(DataBaseManager)} initialized. [{Path.GetFullPath(fileDBPath)}]");
        }

        public void InsertOrUpdate<T>(T obj, string collection)
        {
            // Open database (or create if doesn't exist)
            using (var db = new LiteDatabase(_fileDataBasePath))
            {
                var col = db.GetCollection<T>(collection);

                if(!col.Update(obj))
                    col.Insert(obj);   
            }
        }

        public T GetFirstFromCollection<T>(string collection) where T : class
        {
            using (var db = new LiteDatabase(_fileDataBasePath))
            {
                var col = db.GetCollection<T>(collection);

                var all = new List<T>(col.FindAll());

                if(all.Count > 0)
                    return all[0];

                return null;
            }
        }

        public void InsertOrUpdateDiscordObject<T, DBT>(T discordObj, string collection) where DBT : DBDiscordBaseObject, IDBStorable<T, DBT>, new() where T : SnowflakeObject
        {
            using (var db = new LiteDatabase(_fileDataBasePath))
            {
                var col = db.GetCollection<DBT>(collection);

                var dbObj = col.FindOne(x => x.DiscordUniqueId == discordObj.Id);
                if (dbObj != null)
                {
                    dbObj.CloneFrom(IDBStorable<T,DBT>.FromDiscord(discordObj));
                    col.Update(dbObj);
                    Log.Logger.Warning($"Updated object '{discordObj.Id}' in DB!");
                    return;
                }

                col.Insert(IDBStorable<T, DBT>.FromDiscord(discordObj));
                Log.Logger.Information($"Inserted object '{discordObj.Id}' into DB!");
            }
        }

        public async Task<T> GetDiscordObject<DBT, T>(ulong id, string collection, DiscordClient client) where DBT : DBDiscordBaseObject, IDBRetrievable<DBT, T>, new() where T : SnowflakeObject
        {
            using (var db = new LiteDatabase(_fileDataBasePath))
            {
                var col = db.GetCollection<DBT>(collection);

                var dbObj = col.FindOne(x => x.DiscordUniqueId == id);
                if (dbObj != null)
                {
                    return await IDBRetrievable<DBT, T>.ToDiscord(dbObj, client);
                    //Log.Logger.Warning($"Updated user '{discordObj.Id}' in DB!");
                }
            }
            return null;
        }

        public List<DBT> GetAllInCollection<DBT>(string collection)
        {
            using (var db = new LiteDatabase(_fileDataBasePath))
            {
                var col = db.GetCollection<DBT>(collection);

                var all = col.FindAll();

                if (all == null) return new List<DBT>();

                return new List<DBT>(all);
            }
        }

    }
}
