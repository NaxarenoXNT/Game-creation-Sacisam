using System.Collections.Generic;
using UnityEngine;

namespace World.ChunkSystem
{
    /// <summary>
    /// Datos de un chunk individual del mundo.
    /// Contiene configuraciones de enemigos, props con identidad y exclusiones procedurales.
    /// </summary>
    [System.Serializable]
    public class ChunkData
    {
        [Header("Identificación")]
        public Vector2Int coordinates;
        public string chunkId;
        
        [Header("Configuración de Enemigos")]
        [Tooltip("Configuraciones estáticas de enemigos en este chunk")]
        public List<EnemySpawnConfig> enemySpawnConfigs = new List<EnemySpawnConfig>();
        
        [Header("Props con Identidad")]
        [Tooltip("Objetos con posición fija: edificios, cofres, NPCs, entradas a zonas.")]
        public List<PropSpawnConfig> propSpawnConfigs = new List<PropSpawnConfig>();
        
        [Header("Exclusiones Procedurales")]
        [Tooltip("Zonas donde no se genera vegetación procedural: caminos, plazas, footprints de edificios.")]
        public List<ProceduralExclusion> proceduralExclusions = new List<ProceduralExclusion>();
        
        [Header("Estado Runtime (No Serializado)")]
        [System.NonSerialized] public bool isLoaded;
        [System.NonSerialized] public List<EnemyController> activeEnemies = new List<EnemyController>();
        [System.NonSerialized] public float lastLoadTime;
        [System.NonSerialized] public float lastUnloadTime;
        
        /// <summary>
        /// GameObject padre de toda la decoración procedural y props de este chunk.
        /// Se crea al cargar y se destruye al descargar.
        /// </summary>
        [System.NonSerialized] public Transform propsRoot;
        
        /// <summary>
        /// Instancia del Terrain GameObject creado en runtime desde TerrainData.
        /// Se crea al cargar el chunk y se destruye al descargar.
        /// </summary>
        [System.NonSerialized] public GameObject terrainInstance;
        
        /// <summary>
        /// Estadísticas del chunk.
        /// </summary>
        public ChunkStats GetStats()
        {
            return new ChunkStats
            {
                ChunkId = chunkId,
                Coordinates = coordinates,
                IsLoaded = isLoaded,
                TotalSpawns = enemySpawnConfigs.Count,
                ActiveEnemies = activeEnemies?.Count ?? 0,
                UniqueEnemies = enemySpawnConfigs.FindAll(e => e.isUnique).Count,
                DefeatedUniques = enemySpawnConfigs.FindAll(e => e.isUnique && e.isDefeated).Count
            };
        }
        
        /// <summary>
        /// Obtiene configuraciones que deben spawnearse.
        /// Excluye: únicos derrotados permanentemente + enemigos derrotados esta sesión.
        /// </summary>
        public List<EnemySpawnConfig> GetSpawnableConfigs()
        {
            return enemySpawnConfigs.FindAll(config => 
            {
                // Excluir únicos derrotados permanentemente
                if (config.isUnique && config.isDefeated)
                    return false;
                
                // Excluir enemigos muertos en esta sesión
                if (config.isDefeatedThisSession)
                    return false;
                
                return true;
            });
        }
        
        /// <summary>
        /// Marca un enemigo como derrotado en esta sesión.
        /// </summary>
        public void MarkEnemyDefeated(string spawnId, bool isPermanent = false)
        {
            var config = enemySpawnConfigs.Find(c => c.spawnId == spawnId);
            if (config != null)
            {
                config.isDefeatedThisSession = true;
                
                if (isPermanent && config.isUnique)
                {
                    config.isDefeated = true;
                }
            }
        }
        
        /// <summary>
        /// Remueve un enemigo de la lista de activos.
        /// </summary>
        public void RemoveActiveEnemy(EnemyController controller)
        {
            if (activeEnemies != null)
            {
                activeEnemies.Remove(controller);
            }
        }
        
        /// <summary>
        /// Resetea el estado de la sesión (llamar al reiniciar el juego).
        /// </summary>
        public void ResetSessionState()
        {
            foreach (var config in enemySpawnConfigs)
            {
                config.ResetSessionState();
            }
        }
        
        /// <summary>
        /// Limpia referencias a enemigos activos.
        /// </summary>
        public void ClearActiveReferences()
        {
            activeEnemies?.Clear();
        }
        
        // ─── Props con identidad ─────────────────────────────────────────────────
        
        /// <summary>
        /// Devuelve los PropSpawnConfig que deben spawnearse.
        /// Excluye los consumidos si su estado debe persistir.
        /// </summary>
        public List<PropSpawnConfig> GetSpawnableProps()
        {
            var result = new List<PropSpawnConfig>();
            foreach (var config in propSpawnConfigs)
            {
                if (config.propData == null) continue;
                if (config.isConsumed && config.propData.persistConsumedState) continue;
                result.Add(config);
            }
            return result;
        }
        
        /// <summary>
        /// Marca un prop como consumido.
        /// Llamado desde WorldChunkManager.NotificarPropConsumido().
        /// </summary>
        public void MarkPropConsumed(string propId)
        {
            foreach (var config in propSpawnConfigs)
            {
                if (config.propId == propId)
                {
                    config.isConsumed = true;
                    return;
                }
            }
            Debug.LogWarning($"[ChunkData] PropId '{propId}' no encontrado en chunk {coordinates}");
        }
        
        // ─── Exclusiones procedurales ─────────────────────────────────────────────
        
        /// <summary>
        /// Verifica si una posición está dentro de alguna zona de exclusión del chunk.
        /// </summary>
        public bool IsInExclusionZone(Vector3 worldPos)
        {
            foreach (var exclusion in proceduralExclusions)
            {
                if (exclusion.Contains(worldPos))
                    return true;
            }
            return false;
        }
    }
    
    /// <summary>
    /// Estadísticas de un chunk.
    /// </summary>
    public struct ChunkStats
    {
        public string ChunkId;
        public Vector2Int Coordinates;
        public bool IsLoaded;
        public int TotalSpawns;
        public int ActiveEnemies;
        public int UniqueEnemies;
        public int DefeatedUniques;
    }
}
