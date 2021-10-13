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

        /// <summary>
        /// Formats a TimeSpan HH:mm:ss or mm:ss if the hours component is 0
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="altHoursCheckSource"></param>
        /// <returns></returns>
        public static string SpecialFormatTimeSpan(TimeSpan ts, TimeSpan? altHoursCheckSource = null)
        {
            if(altHoursCheckSource.HasValue)
            {
                return $"{(altHoursCheckSource.Value.Hours > 0 ? $"{ts.Hours.ToString("00")}:" : string.Empty)}{ts.Minutes.ToString("00")}:{ts.Seconds.ToString("00")}";
            }
            return $"{(ts.Hours > 0 ? $"{ts.Hours.ToString("00")}:" : string.Empty)}{ts.Minutes.ToString("00")}:{ts.Seconds.ToString("00")}";
        }

        public static readonly List<string> EmojiNumbersFromOneToTen = new List<string>
        {
            "1️⃣ ",
            "2️⃣ ",
            "3️⃣ ",
            "4️⃣ ",
            "5️⃣ ",
            "6️⃣ ",
            "7️⃣ ",
            "8️⃣ ",
            "9️⃣ ",
            "🔟 "
        };

        /// <summary>
        /// A very beautiful method I must say 🙃
        /// </summary>
        /// <param name="strings"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string GetAlternatingList(List<string> strings, List<string> prefix = null, List<string> suffix = null)
        {
            StringBuilder output = new StringBuilder();
            int count = 0;
            foreach(string s in strings)
            {
                var cc = count;
                if(count++ % 2 == 0)
                {
                    if(prefix != null && prefix.Count > cc)
                    {
                        output.Append(prefix[cc]);
                    }
                    output.Append("**");
                    output.Append(s);
                    output.Append("**");
                    if (suffix != null && suffix.Count > cc)
                    {
                        output.Append(suffix[cc]);
                    }
                    output.Append("\n");
                    continue;
                }
                if (prefix != null && prefix.Count > cc)
                {
                    output.Append(prefix[cc]);
                }
                output.Append(s);
                if (suffix != null && suffix.Count > cc)
                {
                    output.Append(suffix[cc]);
                }
                output.Append("\n");
            }

            return output.ToString();
        }

        public static string GetAlternatingBar(int alternate, string a, string b)
        {
            if (alternate < 0) throw new ArgumentException($"{nameof(alternate)}");

            StringBuilder bar = new StringBuilder();

            for(int i = 0; i < alternate; i++)
            {
                if(i % 2 == 0)
                {
                    bar.Append(b);
                    continue;
                }
                bar.Append(a);
            }

            return bar.ToString();
        }

        public static string GetTextProgressBar(float start, float end, float current, string charFilled, string charPosition, string charEmpty, int stringLength = 20)
        {
            if (stringLength < 5) throw new ArgumentException($"{nameof(stringLength)}");

            StringBuilder progressBar = new StringBuilder();

            var realCurrent = current - start;
            var realEnd = end - start;

            if (realEnd == 0) return "/ by 0";
            var progressPercent = realCurrent / realEnd;

            int filled = (int) Math.Round(stringLength * progressPercent);
            int empty = (int) Math.Round(stringLength * (1-progressPercent));

            if(progressPercent > 1.1f || progressPercent < 0 || filled > stringLength || empty > stringLength)
            {
                return "Error while creating progress bar.";
            }

            for(int i = 0; i < filled; i++)
            {
                progressBar.Append(charFilled);
            }

            progressBar.Append(charPosition);

            for(int i = 0; i < empty - 1; i++)
            {
                progressBar.Append(charEmpty);
            }

            return progressBar.ToString();
        }

        public static bool QuoteUnitTestsQuote()
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
