using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Attributes
{
    public class AutoDI
    {
        [AttributeUsage(AttributeTargets.Class)]
        public class DiscordBotDIA : Attribute
        {

        }

        [AttributeUsage(AttributeTargets.Class)]
        public class Singleton : DiscordBotDIA
        {

        }

        [AttributeUsage(AttributeTargets.Class)]
        public class Transient : DiscordBotDIA
        {

        }

        [AttributeUsage(AttributeTargets.Class)]
        public class Scoped : DiscordBotDIA
        {

        }
    }
}
