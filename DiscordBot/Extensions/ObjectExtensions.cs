using System.Reflection;

namespace DiscordBot.Extensions
{
    public static class ObjectExtensions
    {
        public static void CloneFrom<T>(this object obj, T other)
        {
            foreach (PropertyInfo pi in typeof(T).GetProperties())
            {
                pi.SetValue(obj, pi.GetValue(other));
            }
        }
    }
}
