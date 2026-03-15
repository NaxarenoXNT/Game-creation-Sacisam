using Console.Core;
using Console.Context;
using Console.Adapters;

namespace Console.Commands
{
    public class SpawnCommand : ICommand
    {
        public string Name => "spawn";
        public string Description => "spawn <enemyID> [count] - Spawns enemies by data ID.";

        public CommandResult Execute(string[] args, GameContext context)
        {
            if (args.Length < 1)
                return CommandResult.Fail("Usage: spawn <enemyID> [count]");

            string enemyId = args[0];
            int count = 1;

            if (args.Length >= 2)
            {
                if (!int.TryParse(args[1], out count) || count < 1)
                    return CommandResult.Fail("Error: count must be a valid integer greater than 0.");
            }

            if (!context.EnemySpawner.EnemyExists(enemyId))
            {
                string available = (context.EnemySpawner is EnemySpawnerAdapter adapter)
                    ? $" Available: {adapter.GetAvailableEnemyIds()}"
                    : "";
                return CommandResult.Fail($"Error: enemy \"{enemyId}\" not found.{available}");
            }

            bool success = context.EnemySpawner.Spawn(enemyId, count);
            if (!success)
                return CommandResult.Fail($"Error: failed to spawn \"{enemyId}\".");

            return CommandResult.Ok($"Spawned {count} {enemyId}.");
        }
    }
}
