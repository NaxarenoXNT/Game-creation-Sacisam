using System.Text;
using Console.Core;
using Console.Context;

namespace Console.Commands
{
    public class HelpCommand : ICommand
    {
        private readonly CommandRegistry _registry;

        public string Name => "help";
        public string Description => "help - Lists all available commands.";

        public HelpCommand(CommandRegistry registry)
        {
            _registry = registry;
        }

        public CommandResult Execute(string[] args, GameContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Available commands:");

            foreach (var kvp in _registry.GetAllCommands())
            {
                sb.AppendLine($"  {kvp.Value.Description}");
            }

            return CommandResult.Ok(sb.ToString().TrimEnd());
        }
    }
}
