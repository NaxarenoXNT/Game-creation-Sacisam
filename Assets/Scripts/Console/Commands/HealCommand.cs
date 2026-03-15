using Console.Core;
using Console.Context;

namespace Console.Commands
{
    public class HealCommand : ICommand
    {
        public string Name => "heal";
        public string Description => "heal - Restores player to full health.";

        public CommandResult Execute(string[] args, GameContext context)
        {
            context.PlayerProgression.HealToFull();
            int hp = context.PlayerProgression.MaxHealth;
            return CommandResult.Ok($"Player healed to full health ({hp}/{hp}).");
        }
    }
}
