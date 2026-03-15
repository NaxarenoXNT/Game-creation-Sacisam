using Console.Context;

namespace Console.Core
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        CommandResult Execute(string[] args, GameContext context);
    }
}
