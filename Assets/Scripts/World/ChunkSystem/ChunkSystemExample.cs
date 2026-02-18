using UnityEngine;
using World.ChunkSystem;

/// <summary>
/// Ejemplo de uso del sistema de chunks.
/// Este script muestra cómo trabajar con el sistema programáticamente.
/// </summary>
public class ChunkSystemExample : MonoBehaviour
{
    [Header("Ejemplo 1: Crear Chunk Dinámicamente")]
    [SerializeField] private EnemigoData goblinData;
    [SerializeField] private EnemigoData dragonData;
    
    void Start()
    {
        // Esperar a que el manager se inicialice
        Invoke(nameof(EjemploCrearChunkDinamico), 1f);
    }
    
    /// <summary>
    /// Ejemplo: Crear un chunk con enemigos programáticamente.
    /// </summary>
    void EjemploCrearChunkDinamico()
    {
        if (WorldChunkManager.Instance == null)
        {
            Debug.LogError("WorldChunkManager no encontrado");
            return;
        }
        
        // Crear chunk en coordenadas (0, 0)
        var chunk = new ChunkData
        {
            coordinates = new Vector2Int(0, 0),
            chunkId = "chunk_test_00",
            enemySpawnConfigs = new System.Collections.Generic.List<EnemySpawnConfig>()
        };
        
        // Agregar 3 goblins en patrulla
        for (int i = 0; i < 3; i++)
        {
            var config = new EnemySpawnConfig
            {
                spawnId = $"goblin_patrol_{i}",
                enemyData = goblinData,
                spawnPosition = new Vector3(10 + i * 5, 0, 10),
                initialAIState = EnemyAIState.Patrolling,
                patrolBehavior = PatrolBehavior.Loop,
                patrolWaypoints = new System.Collections.Generic.List<Vector3>
                {
                    new Vector3(10 + i * 5, 0, 10),
                    new Vector3(20 + i * 5, 0, 10),
                    new Vector3(20 + i * 5, 0, 20),
                    new Vector3(10 + i * 5, 0, 20)
                }
            };
            
            chunk.enemySpawnConfigs.Add(config);
        }
        
        // Agregar un boss en el centro de ejemplo ya que no existe pero para mas adelnate
        var bossConfig = new EnemySpawnConfig
        {
            spawnId = "dragon_boss",
            enemyData = dragonData,
            spawnPosition = new Vector3(50, 0, 50),
            initialAIState = EnemyAIState.Resting,
            isUnique = true,
            uniqueId = "dragon_king_001",
            detectionRadius = 30f,
            chaseRadius = 50f
        };
        
        chunk.enemySpawnConfigs.Add(bossConfig);
        
        // Registrar chunk
        WorldChunkManager.Instance.RegisterChunk(chunk);
        
        Debug.Log($"✅ Chunk {chunk.chunkId} creado con {chunk.enemySpawnConfigs.Count} spawns");
    }
    
    /// <summary>
    /// Ejemplo: Obtener información de un chunk.
    /// </summary>
    [ContextMenu("Ejemplo: Info Chunk Actual")]
    void EjemploObtenerInfoChunk()
    {
        if (WorldChunkManager.Instance == null) return;
        
        var playerPos = UnityEngine.Camera.main.transform.position;
        var chunkCoords = WorldChunkManager.Instance.WorldToChunkCoords(playerPos);
        var chunk = WorldChunkManager.Instance.GetChunk(chunkCoords);
        
        if (chunk != null)
        {
            var stats = chunk.GetStats();
            Debug.Log($"📊 Chunk {stats.ChunkId}");
            Debug.Log($"   Coordenadas: {stats.Coordinates}");
            Debug.Log($"   Cargado: {stats.IsLoaded}");
            Debug.Log($"   Spawns totales: {stats.TotalSpawns}");
            Debug.Log($"   Enemigos activos: {stats.ActiveEnemies}");
            Debug.Log($"   Enemigos únicos: {stats.UniqueEnemies}");
        }
        else
        {
            Debug.Log($"Chunk {chunkCoords} no existe");
        }
    }
    
    /// <summary>
    /// Ejemplo: Marcar un enemigo único como derrotado.
    /// </summary>
    public void MarcarBossDerrotado(string uniqueId)
    {
        // Buscar en todos los chunks
        foreach (var chunk in WorldChunkManager.Instance.GetType()
            .GetField("chunks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(WorldChunkManager.Instance) as System.Collections.Generic.Dictionary<Vector2Int, ChunkData>)
        {
            var config = chunk.Value.enemySpawnConfigs.Find(c => c.uniqueId == uniqueId);
            if (config != null)
            {
                config.isDefeated = true;
                Debug.Log($"✅ Boss {uniqueId} marcado como derrotado");
                return;
            }
        }
        
        Debug.LogWarning($"⚠️ Boss {uniqueId} no encontrado");
    }
}
