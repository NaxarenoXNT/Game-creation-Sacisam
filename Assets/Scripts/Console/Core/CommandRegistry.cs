using System;
using System.Collections.Generic;
using Console.Context;

namespace Console.Core
{
    public class CommandRegistry
    {
        private readonly Dictionary<string, ICommand> _commands = new Dictionary<string, ICommand>();
        private readonly GameContext _context;

        public CommandRegistry(GameContext context)
        {
            _context = context;
        }

        public void Register(ICommand command)
        {
            _commands[command.Name.ToLowerInvariant()] = command;
        }

        public CommandResult ExecuteRaw(string input)
        {
            var (commandName, args) = CommandParser.Parse(input);

            if (string.IsNullOrEmpty(commandName))
                return CommandResult.Fail("Empty command.");

            if (!_commands.TryGetValue(commandName, out ICommand command))
                return CommandResult.Fail($"Unknown command: \"{commandName}\". Type \"help\" for a list of commands.");

            try
            {
                return command.Execute(args, _context);
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Internal error: " + ex.Message);
            }
        }

        public IReadOnlyDictionary<string, ICommand> GetAllCommands() => _commands;
    }
}
