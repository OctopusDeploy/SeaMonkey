using System.ComponentModel.Design;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeaMonkey.Models;

namespace SeaMonkey.Commands
{
    public abstract class CommandBase : ICommand
    {
        protected Options Options { get; } = new Options();

        public async Task Execute(string[] args)
        {
            Options.Parse(args);
            await Execute();
        }

        public abstract Task Execute();
    }
    
    [Command("create-empty-profile")]
    public class CreateProfileCommand : CommandBase
    {
        private string file;
        private bool overwrite;

        public CreateProfileCommand()
        {
            var optionSet = Options.For("Create Profile");
            optionSet.Add("file=", "Output file", f => this.file = f);
            optionSet.Add("overwrite", "overwrite existing file", o => this.overwrite = true);
        }

        public override Task Execute()
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                throw new CommandException("File name not provided");
            }

            if (File.Exists(file) && !overwrite)
            {
                throw new CommandException("File already exists, use --overwrite to replace");
            }

            var profile = new SeamonkeyProfile();

            File.WriteAllText(file, JsonConvert.SerializeObject(profile, Formatting.Indented));

            return Task.CompletedTask;
        }
    }
}