using System;

namespace SeaMonkey.Commands
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CommandAttribute : Attribute
    {
        public CommandAttribute(string name)
        {
            Name = name;
        }

        public CommandAttribute(string name, string preParseOptionName, string preParseOptionDescription) : this(name)
        {
            PreParseOptionName = preParseOptionName;
            PreParseOptionDescription = preParseOptionDescription;
        }

        public string Name { get; }
        public string PreParseOptionName { get; }
        public string PreParseOptionDescription { get; }
    }
}