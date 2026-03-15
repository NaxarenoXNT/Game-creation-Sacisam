namespace Console.Context
{
    public interface IEnemySpawner
    {
        /// <summary>
        /// Spawns enemies by data ID. Returns true if the ID was found and spawn succeeded.
        /// </summary>
        bool Spawn(string enemyId, int count);

        /// <summary>
        /// Returns true if an enemy with the given data ID exists.
        /// </summary>
        bool EnemyExists(string enemyId);

        /// <summary>
        /// Kills all active enemies in the scene.
        /// </summary>
        void KillAll();
    }
}
