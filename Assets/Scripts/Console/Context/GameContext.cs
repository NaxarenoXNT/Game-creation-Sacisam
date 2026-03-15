namespace Console.Context
{
    public class GameContext
    {
        public IEnemySpawner EnemySpawner { get; }
        public IInventorySystem InventorySystem { get; }
        public IPlayerProgression PlayerProgression { get; }

        public GameContext(
            IEnemySpawner enemySpawner,
            IInventorySystem inventorySystem,
            IPlayerProgression playerProgression)
        {
            EnemySpawner = enemySpawner;
            InventorySystem = inventorySystem;
            PlayerProgression = playerProgression;
        }
    }
}
