using System;

namespace DiscordBot.Attributes
{
    public class AttachedStringAttribute : Attribute
    {
        public string Value { get; private set; }
        public AttachedStringAttribute(string value)
        {
            Value = value;
        }
    }
}
