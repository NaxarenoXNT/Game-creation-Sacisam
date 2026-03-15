using Console.Core;
using Console.Context;

namespace Console.Commands
{
    public class LevelUpCommand : ICommand
    {
        public string Name => "levelup";
        public string Description => "levelup [amount] - Increases player level.";

        public CommandResult Execute(string[] args, GameContext context)
        {
            int amount = 1;

            if (args.Length >= 1)
            {
                if (!int.TryParse(args[0], out amount) || amount < 1)
                    return CommandResult.Fail("Error: amount must be a valid positive integer.");
            }

            int previousLevel = context.PlayerProgression.CurrentLevel;
            context.PlayerProgression.LevelUp(amount);
            int newLevel = context.PlayerProgression.CurrentLevel;

            return CommandResult.Ok($"Level up! {previousLevel} -> {newLevel}.");
        }
    }
}
