using Console.Core;
using Console.Context;

namespace Console.Commands
{
    public class KillAllCommand : ICommand
    {
        public string Name => "killall";
        public string Description => "killall - Removes all active enemies.";

        public CommandResult Execute(string[] args, GameContext context)
        {
            context.EnemySpawner.KillAll();
            return CommandResult.Ok("All enemies killed.");
        }
    }
}
