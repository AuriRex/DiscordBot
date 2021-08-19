using DiscordBot.Extensions;
using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using DiscordBot.Models.Database.Discord;
using System.Text.Json;

namespace DiscordBot
{
    public class Utilities
    {
        public static JsonSerializerOptions JsonOptions { get; set; } = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Loads a file on the specified <paramref name="path"/> and returns a deserialized object <paramref name="jsonObject"/> if the file exists or creates a new (default) one if it does not.
        /// </summary>
        /// <typeparam name="T">The type of the object to create</typeparam>
        /// <param name="path">Path to the file to read</param>
        /// <param name="jsonObject">The generated object</param>
        /// <returns><paramref name="true"/> if a read is successful, <paramref name="false"/> if a new object is created</returns>
        /// <param name="options">(Optional) serializer options, a default is used if this is null</param>
        public static bool TryLoadJSON<T>(string path, out T jsonObject, JsonSerializerOptions options = null) where T : new()
        {
            if (!File.Exists(path))
            {
                jsonObject = new T();
                return false;
            }

            string jsonString = File.ReadAllText(path);

            jsonObject = JsonSerializer.Deserialize<T>(jsonString, options ?? JsonOptions);
            return true;
        }

        /// <summary>
        /// Save an object of type <typeparamref name="T"/> into a file at path <paramref name="path"/> serialized as JSON
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize</typeparam>
        /// <param name="jsonObject">The instance of the object to serialize</param>
        /// <param name="path">The file path to save to</param>
        /// <param name="options">(Optional) serializer options, a default is used if this is null</param>
        public static void SaveJSON<T>(T jsonObject, string path, JsonSerializerOptions options = null)
        {
            string jsonString = JsonSerializer.Serialize<T>(jsonObject, options ?? JsonOptions);

            File.WriteAllText(path, jsonString);
        }

        public static bool Tests()
        {
            var user1 = new DBDiscordUser()
            {
                DiscordUniqueId = 12345,
                Username = "Userhooman",
                Discriminator = "1357"
            };

            var user2 = new DBDiscordUser();

            user2.CloneFrom(user1);

            if (user2.DiscordUniqueId != user1.DiscordUniqueId
                || user2.Discriminator != user1.Discriminator
                || user2.Username != user1.Username)
                return false;

            return true;
        }
    }
}
