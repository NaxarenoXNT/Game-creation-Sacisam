using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace World.ChunkSystem
{
    /// <summary>
    /// Gestiona el spawning y despawning de enemigos en chunks.
    /// Se integra con DynamicEnemyPoolManager para obtener/devolver controllers.
    /// </summary>
    public class ChunkEnemySpawner
    {
        private readonly DynamicEnemyPoolManager enemyPoolManager;
        private readonly System.Func<Vector3, Vector2Int> worldToChunkCoords;

        public ChunkEnemySpawner(DynamicEnemyPoolManager poolManager, System.Func<Vector3, Vector2Int> worldToChunkCoords)
        {
            this.enemyPoolManager = poolManager;
            this.worldToChunkCoords = worldToChunkCoords;
        }

        /// <summary>
        /// Spawnea enemigos de un chunk de forma escalonada.
        /// </summary>
        public IEnumerator SpawnEnemiesCoroutine(ChunkData chunk, int maxSpawnsPerFrame, bool showDebugLogs)
        {
            if (enemyPoolManager == null)
            {
                Debug.LogError("❌ DynamicEnemyPoolManager no está asignado");
                yield break;
            }
            
            var spawnableConfigs = chunk.GetSpawnableConfigs();
            
            if (showDebugLogs)
            {
                Debug.Log($"📋 Chunk {chunk.coordinates}: {spawnableConfigs.Count} configs spawneables de {chunk.enemySpawnConfigs.Count} totales");
            }
            
            if (spawnableConfigs.Count == 0)
            {
                if (showDebugLogs && chunk.enemySpawnConfigs.Count > 0)
                {
                    Debug.LogWarning($"⚠️ Chunk {chunk.coordinates} tiene {chunk.enemySpawnConfigs.Count} configs pero 0 spawneables. " +
                                    "¿Están todos derrotados o mal configurados?");
                }
                yield break;
            }
            
            int spawned = 0;
            
            foreach (var config in spawnableConfigs)
            {
                var controller = SpawnEnemy(config);
                
                if (controller != null)
                {
                    chunk.activeEnemies.Add(controller);
                    spawned++;
                    
                    // Limitar spawns por frame
                    if (spawned % maxSpawnsPerFrame == 0)
                    {
                        yield return null;
                        if (!chunk.isLoaded) yield break;
                    }
                }
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"✅ Chunk {chunk.coordinates}: {spawned}/{spawnableConfigs.Count} enemigos spawneados correctamente");
            }
        }
        
        /// <summary>
        /// Spawnea un enemigo individual desde configuración.
        /// </summary>
        public EnemyController SpawnEnemy(EnemySpawnConfig config)
        {
            if (config.enemyData == null)
            {
                Debug.LogWarning($"⚠️ EnemySpawnConfig sin EnemigoData: {config.spawnId}");
                return null;
            }
            
            // Obtener controller del pool
            var controller = enemyPoolManager.ObtenerController(config.enemyData);
            
            if (controller == null)
            {
                Debug.LogError($"❌ No se pudo obtener controller para {config.enemyData.name}");
                return null;
            }
            
            // Configurar posición y rotación
            controller.transform.position = config.spawnPosition;
            controller.transform.rotation = config.spawnRotation;
            
            // ✅ INICIALIZAR CON DATOS DEL ENEMIGO, CHUNK Y SPAWN CONFIG
            var chunkCoords = worldToChunkCoords(config.spawnPosition);
            controller.InicializarDesdeChunk(config.enemyData, config.spawnId, chunkCoords, config);
            
            // Guardar referencia en el config
            config.activeController = controller;
            
            // Activar
            controller.gameObject.SetActive(true);
            
            // ✅ Sistema de IA implementado - el FSM se inicializa automáticamente en InicializarDesdeChunk
            
            return controller;
        }

        /// <summary>
        /// Devuelve enemigos activos vivos de un chunk al pool.
        /// </summary>
        public void ReturnEnemiesToPool(ChunkData chunkData)
        {
            var enemiesToReturn = new List<EnemyController>(chunkData.activeEnemies);
            foreach (var enemy in enemiesToReturn)
            {
                if (enemy != null && enemy.EstaVivo() && enemyPoolManager != null)
                {
                    enemyPoolManager.DevolverController(enemy, enemy.DatosEnemigo);
                }
            }
        }

        /// <summary>
        /// Devuelve un controller al pool después de un delay.
        /// </summary>
        public IEnumerator ReturnControllerToPoolCoroutine(EnemyController controller, float delay, bool showDebugLogs)
        {
            yield return new WaitForSeconds(delay);
            
            if (controller != null && enemyPoolManager != null)
            {
                enemyPoolManager.DevolverController(controller, controller.DatosEnemigo);
                
                if (showDebugLogs)
                {
                    Debug.Log($"♻️ Controller devuelto al pool: {controller.DatosEnemigo?.name}");
                }
            }
        }
        
    }
}
