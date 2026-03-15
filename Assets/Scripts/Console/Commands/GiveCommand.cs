using Console.Core;
using Console.Context;

namespace Console.Commands
{
    public class GiveCommand : ICommand
    {
        public string Name => "give";
        public string Description => "give <itemID> [amount] - Gives an item to the player.";

        public CommandResult Execute(string[] args, GameContext context)
        {
            if (args.Length < 1)
                return CommandResult.Fail("Usage: give <itemID> [amount]");

            if (context.InventorySystem == null)
                return CommandResult.Fail("Error: inventory system is not available.");

            string itemId = args[0];
            int amount = 1;

            if (args.Length >= 2)
            {
                if (!int.TryParse(args[1], out amount) || amount < 1)
                    return CommandResult.Fail("Error: amount must be a valid integer greater than 0.");
            }

            if (!context.InventorySystem.ItemExists(itemId))
                return CommandResult.Fail($"Error: item \"{itemId}\" not found.");

            bool success = context.InventorySystem.GiveItem(itemId, amount);
            if (!success)
                return CommandResult.Fail($"Error: failed to give \"{itemId}\".");

            return CommandResult.Ok($"Gave {amount} {itemId}.");
        }
    }
}
