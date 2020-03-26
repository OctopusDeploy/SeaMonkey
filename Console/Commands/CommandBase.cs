using System.Threading.Tasks;

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
}