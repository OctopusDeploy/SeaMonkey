using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;

namespace SeaMonkey.Commands
{
    public interface ICommand
    {
        Task Execute(string[] args);
    }
    
    public interface ICommandLocator
    {
        IReadOnlyList<(CommandAttribute attribute, Type type)> List();
        ICommand Find(string name, string[] args);
    }
    
    public class CommandLocator : ICommandLocator
    {
        readonly ILifetimeScope lifetimeScope;

        public CommandLocator(ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
        }

        public IReadOnlyList<(CommandAttribute attribute, Type type)> List()
        {
            var iCommandType = typeof(ICommand);
            return
                (from t in typeof(CommandLocator).Assembly.GetTypes()
                    where iCommandType.IsAssignableFrom(t)
                    let attribute =
                        (CommandAttribute) t.GetCustomAttributes(typeof(CommandAttribute), true).FirstOrDefault()
                    where attribute != null
                    select (attribute: attribute, type: t)).ToArray();
        }

        public ICommand Find(string name, string[] args)
        {
            name = name.Trim().ToLowerInvariant();

            // search the provided assemblies in order
            var found = List().FirstOrDefault(a => a.attribute.Name == name);

            if (found.attribute == null)
                return null;

            return found.attribute.PreParseOptionName == null
                ? (ICommand) lifetimeScope.Resolve(found.type)
                : (ICommand) lifetimeScope.Resolve(found.type,
                    new List<Parameter>
                    {
                        new NamedParameter(found.attribute.PreParseOptionName, FindPreParseOptionValue(args, found.attribute.PreParseOptionName))
                    });
        }

        static string FindPreParseOptionValue(string[] commandLineArguments, string preParseOptionName)
        {
            var preParsedOptionValue = string.Empty;
            var options = new OptionSet().Add(preParseOptionName + "=", p => preParsedOptionValue = p);

            // Ignore the return parameter here, we want to leave the instance option for the responsible command
            // We're just peeking to see if we can load the instance as early as possible
            options.Parse(commandLineArguments);
            return preParsedOptionValue;
        }
    }
}