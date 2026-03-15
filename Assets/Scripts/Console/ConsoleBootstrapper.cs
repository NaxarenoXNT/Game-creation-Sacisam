using UnityEngine;
using Console.Core;
using Console.Context;
using Console.Commands;
using Console.Adapters;
using Console.UI;

namespace Console
{
    /// <summary>
    /// Bootstrapper de la consola de desarrollador.
    /// Debe estar en el mismo GameObject que ConsoleUI y el UIDocument.
    /// Auto-detecta los sistemas del juego en Start() y construye los adapters.
    /// </summary>
    [RequireComponent(typeof(ConsoleUI))]
    public class ConsoleBootstrapper : MonoBehaviour
    {
        private void Start()
        {
            // Crear adapters que conectan con los sistemas reales del juego
            var enemySpawner      = new EnemySpawnerAdapter();
            var playerProgression = new PlayerProgressionAdapter();

            // IInventorySystem no existe todavía — se pasa null
            IInventorySystem inventorySystem = null;

            // Construir contexto y registro
            var context  = new GameContext(enemySpawner, inventorySystem, playerProgression);
            var registry = new CommandRegistry(context);

            RegisterCommands(registry);

            GetComponent<ConsoleUI>().Initialize(registry);

            Debug.Log("[Console] Developer console initialized.");
        }

        private void RegisterCommands(CommandRegistry registry)
        {
            registry.Register(new SpawnCommand());
            registry.Register(new GiveCommand());
            registry.Register(new LevelUpCommand());
            registry.Register(new HealCommand());
            registry.Register(new KillAllCommand());
            registry.Register(new HelpCommand(registry));
        }
    }
}
