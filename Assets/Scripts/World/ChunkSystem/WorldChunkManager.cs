using System.Collections.Generic;
using UnityEngine;
using Managers;

namespace World.ChunkSystem
{
    /// <summary>
    /// Manager central del sistema de chunks.
    /// Gestiona carga/descarga de chunks y spawning de enemigos.
    /// Se integra con DynamicEnemyPoolManager para obtener controllers.
    /// </summary>
    public class WorldChunkManager : MonoBehaviour
    {
        public static WorldChunkManager Instance { get; private set; }
        
        [Header("Referencias")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private DynamicEnemyPoolManager enemyPoolManager;
        
        [Header("Configuración de Chunks")]
        [Tooltip("Tamaño de cada chunk en unidades (ESTE ES EL VALOR MAESTRO usado por todo el sistema)")]
        [SerializeField] private float chunkSize = 256f;
        
        /// <summary>
        /// Tamaño del chunk en unidades. Este es el valor maestro que todos los sistemas deben usar.
        /// </summary>
        public float ChunkSize => chunkSize;
        
        [Tooltip("Radio de carga en chunks (1 = solo chunk actual, 2 = chunk + vecinos, etc.)")]
        [SerializeField] private int loadRadius = 4;
        
        [Tooltip("Tiempo entre checks de carga/descarga (segundos)")]
        [SerializeField] private float updateInterval = 1f;
        
        [Header("Optimización")]
        [Tooltip("Tiempo mínimo antes de poder recargar un chunk (segundos)")]
        [SerializeField] private float minReloadTime = 5f;
        
        [Tooltip("Máximo de enemigos a spawnear por frame")]
        [SerializeField] private int maxSpawnsPerFrame = 5;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool showDebugLogs = true;
        
        // Estado interno
        private Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();
        private HashSet<Vector2Int> loadedChunks = new HashSet<Vector2Int>();
        private Vector2Int currentPlayerChunk;
        private Vector2Int lastPlayerChunk;
        private float lastUpdateTime;
        
        // Propiedades públicas
        public int TotalChunks => chunks.Count;
        public int LoadedChunksCount => loadedChunks.Count;
        public int TotalActiveEnemies
        {
            get
            {
                int total = 0;
                foreach (var chunk in chunks.Values)
                {
                    if (chunk.isLoaded)
                        total += chunk.activeEnemies?.Count ?? 0;
                }
                return total;
            }
        }
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        void Start()
        {
            // Auto-detectar referencias si no están asignadas
            if (playerTransform == null)
            {
                // Intentar obtener del PlayerPartyManager primero
                if (PlayerPartyManager.Instance != null)
                {
                    playerTransform = PlayerPartyManager.Instance.MainTransform;
                }
                
                // Fallback: buscar por tag
                if (playerTransform == null)
                {
                    var player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                        playerTransform = player.transform;
                }
            }
            
            if (enemyPoolManager == null)
            {
                enemyPoolManager = DynamicEnemyPoolManager.Instance;
            }
            
            // ✅ Auto-cargar ChunkDataAssets desde Resources/World/Chunks/
            AutoLoadChunkAssets();
            
            if (showDebugLogs)
            {
                Debug.Log($"✨ WorldChunkManager inicializado | ChunkSize: {chunkSize} | LoadRadius: {loadRadius}");
            }
            
            // ✅ ARREGLO: Cargar chunks iniciales inmediatamente
            if (playerTransform != null)
            {
                currentPlayerChunk = WorldToChunkCoords(playerTransform.position);
                lastPlayerChunk = currentPlayerChunk; // sincronizado para que Update no recargue de inmediato
                UpdateLoadedChunks(); // Cargar chunks iniciales
                
                if (showDebugLogs)
                {
                    Debug.Log($"🗺️ Chunks iniciales cargados. Player en chunk: {currentPlayerChunk}");
                }
            }
            else
            {
                Debug.LogWarning("[WorldChunkManager] playerTransform es null en Start(). " +
                                 "Se intentará resolver en Update(). " +
                                 "Verifica que el campo playerTransform en el Inspector esté vacío " +
                                 "o que el player tenga el tag 'Player'.");
            }
        }
        
        /// <summary>
        /// Carga automáticamente todos los ChunkDataAsset desde Resources/World/Chunks/.
        /// Esto registra los chunks con sus configuraciones de enemigos al iniciar.
        /// </summary>
        private void AutoLoadChunkAssets()
        {
            var chunkAssets = Resources.LoadAll<ChunkDataAsset>("World/Chunks");
            
            if (chunkAssets == null || chunkAssets.Length == 0)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning("⚠️ No se encontraron ChunkDataAssets en Resources/World/Chunks/");
                }
                return;
            }
            
            int loaded = 0;
            int withEnemies = 0;
            
            foreach (var asset in chunkAssets)
            {
                if (asset != null)
                {
                    var runtimeData = asset.ToRuntimeData();
                    
                    if (!chunks.ContainsKey(runtimeData.coordinates))
                    {
                        chunks[runtimeData.coordinates] = runtimeData;
                        loaded++;
                        
                        if (runtimeData.enemySpawnConfigs.Count > 0)
                        {
                            withEnemies++;
                        }
                    }
                }
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"📦 Auto-carga: {loaded} chunks registrados ({withEnemies} con enemigos) desde Resources/World/Chunks/");
            }
        }
        
        void Update()
        {
            // Si playerTransform es null (fallo de timing en Start), intentar resolverlo cada tick
            if (playerTransform == null)
            {
                if (PlayerPartyManager.Instance != null)
                    playerTransform = PlayerPartyManager.Instance.MainTransform;

                if (playerTransform == null)
                {
                    var player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                        playerTransform = player.transform;
                }

                // Si lo encontramos ahora, hacer la carga inicial que se perdió
                if (playerTransform != null)
                {
                    currentPlayerChunk = WorldToChunkCoords(playerTransform.position);
                    lastPlayerChunk = currentPlayerChunk;
                    UpdateLoadedChunks();
                    Debug.Log($"[WorldChunkManager] playerTransform resuelto tardío. Player en chunk: {currentPlayerChunk}");
                }

                return;
            }
            
            // Update periódico
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateChunks();
            }
        }
        
        /// <summary>
        /// Actualiza chunks según posición del jugador.
        /// </summary>
        private void UpdateChunks()
        {
            currentPlayerChunk = WorldToChunkCoords(playerTransform.position);
            
            // Solo actualizar si el jugador cambió de chunk
            if (currentPlayerChunk != lastPlayerChunk)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"🗺️ Jugador cambió de chunk: {lastPlayerChunk} → {currentPlayerChunk}");
                }
                
                UpdateLoadedChunks();
                lastPlayerChunk = currentPlayerChunk;
            }
        }
        
        /// <summary>
        /// Actualiza qué chunks deben estar cargados.
        /// </summary>
        private void UpdateLoadedChunks()
        {
            var chunksToLoad = new HashSet<Vector2Int>();
            
            // Calcular chunks que deben estar cargados
            for (int x = -loadRadius; x <= loadRadius; x++)
            {
                for (int y = -loadRadius; y <= loadRadius; y++)
                {
                    var chunkCoords = currentPlayerChunk + new Vector2Int(x, y);
                    chunksToLoad.Add(chunkCoords);
                }
            }
            
            // Descargar chunks fuera de rango
            var chunksToUnload = new List<Vector2Int>();
            foreach (var loadedChunk in loadedChunks)
            {
                if (!chunksToLoad.Contains(loadedChunk))
                {
                    chunksToUnload.Add(loadedChunk);
                }
            }
            
            foreach (var chunkCoords in chunksToUnload)
            {
                UnloadChunk(chunkCoords);
            }
            
            // Cargar nuevos chunks
            foreach (var chunkCoords in chunksToLoad)
            {
                if (!loadedChunks.Contains(chunkCoords))
                {
                    LoadChunk(chunkCoords);
                }
            }
        }
        
        /// <summary>
        /// Carga un chunk (spawnea enemigos).
        /// </summary>
        private void LoadChunk(Vector2Int coords)
        {
            if (!chunks.TryGetValue(coords, out var chunkData))
            {
                // Chunk no existe, crear vacío
                chunkData = CreateEmptyChunk(coords);
                chunks[coords] = chunkData;
            }
            
            // Verificar tiempo mínimo de recarga
            if (Time.time - chunkData.lastUnloadTime < minReloadTime)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"⏳ Chunk {coords} en cooldown de recarga");
                }
                return;
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"📦 Cargando chunk {coords} ({chunkData.enemySpawnConfigs.Count} spawns)");
            }
            
            // Spawnear enemigos
            StartCoroutine(SpawnEnemiesCoroutine(chunkData));
            
            chunkData.isLoaded = true;
            chunkData.lastLoadTime = Time.time;
            loadedChunks.Add(coords);
        }
        
        /// <summary>
        /// Descarga un chunk (devuelve enemigos al pool).
        /// </summary>
        private void UnloadChunk(Vector2Int coords)
        {
            if (!chunks.TryGetValue(coords, out var chunkData))
                return;
            
            if (!chunkData.isLoaded)
                return;
            
            if (showDebugLogs)
            {
                Debug.Log($"📤 Descargando chunk {coords} ({chunkData.activeEnemies.Count} enemigos activos)");
            }
            
            // Devolver enemigos al pool (solo los que están vivos)
            var enemiesToReturn = new List<EnemyController>(chunkData.activeEnemies);
            foreach (var enemy in enemiesToReturn)
            {
                if (enemy != null && enemy.EstaVivo() && enemyPoolManager != null)
                {
                    enemyPoolManager.DevolverController(enemy, enemy.DatosEnemigo);
                }
            }
            
            chunkData.ClearActiveReferences();
            chunkData.isLoaded = false;
            chunkData.lastUnloadTime = Time.time;
            loadedChunks.Remove(coords);
        }
        
        /// <summary>
        /// Spawnea enemigos de un chunk de forma escalonada.
        /// </summary>
        private System.Collections.IEnumerator SpawnEnemiesCoroutine(ChunkData chunk)
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
                // Spawnear enemigo
                var controller = SpawnEnemy(config);
                
                if (controller != null)
                {
                    chunk.activeEnemies.Add(controller);
                    spawned++;
                    
                    // Limitar spawns por frame
                    if (spawned % maxSpawnsPerFrame == 0)
                    {
                        yield return null;
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
        private EnemyController SpawnEnemy(EnemySpawnConfig config)
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
            var chunkCoords = WorldToChunkCoords(config.spawnPosition);
            controller.InicializarDesdeChunk(config.enemyData, config.spawnId, chunkCoords, config);
            
            // Guardar referencia en el config
            config.activeController = controller;
            
            // Activar
            controller.gameObject.SetActive(true);
            
            // ✅ Sistema de IA implementado - el FSM se inicializa automáticamente en InicializarDesdeChunk
            
            return controller;
        }
        
        /// <summary>
        /// Aplica la configuración de IA del EnemySpawnConfig al controller.
        /// TODO: Implementar cuando tengas el sistema de IA.
        /// </summary>
        private void ApplyAIConfiguration(EnemyController controller, EnemySpawnConfig config)
        {
            // Ejemplo de lo que podrías hacer:
            
            // 1. Configurar estado inicial de IA
            // controller.SetAIState(config.initialAIState);
            
            // 2. Configurar patrulla si tiene waypoints
            // if (config.patrolWaypoints.Count > 0)
            // {
            //     controller.SetPatrolRoute(config.patrolWaypoints, config.patrolBehavior);
            //     controller.SetPatrolSpeed(config.patrolSpeed > 0 ? config.patrolSpeed : controller.DefaultSpeed);
            //     controller.SetWaypointWaitTime(config.waypointWaitTime);
            // }
            
            // 3. Configurar radios de detección
            // if (config.detectionRadius > 0)
            //     controller.SetDetectionRadius(config.detectionRadius);
            // if (config.chaseRadius > 0)
            //     controller.SetChaseRadius(config.chaseRadius);
            
            // 4. Aplicar tags personalizados
            // foreach (var tag in config.customTags)
            // {
            //     controller.AddTag(tag);
            // }
            
            // 5. Aplicar datos personalizados
            // foreach (var data in config.customData)
            // {
            //     controller.SetCustomData(data.key, data.value);
            // }
        }
        
        /// <summary>
        /// Convierte coordenadas del mundo a coordenadas de chunk.
        /// </summary>
        public Vector2Int WorldToChunkCoords(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.x / chunkSize);
            int z = Mathf.FloorToInt(worldPos.z / chunkSize);
            return new Vector2Int(x, z);
        }
        
        /// <summary>
        /// Convierte coordenadas de chunk a posición central del mundo.
        /// </summary>
        public Vector3 ChunkToWorldPos(Vector2Int chunkCoords)
        {
            float x = (chunkCoords.x + 0.5f) * chunkSize;
            float z = (chunkCoords.y + 0.5f) * chunkSize;
            return new Vector3(x, 0, z);
        }
        
        /// <summary>
        /// Registra un chunk con configuración de enemigos.
        /// </summary>
        public void RegisterChunk(ChunkData chunk)
        {
            if (chunk == null) return;
            
            if (chunks.ContainsKey(chunk.coordinates))
            {
                Debug.LogWarning($"⚠️ Chunk {chunk.coordinates} ya está registrado");
                return;
            }
            
            chunks[chunk.coordinates] = chunk;
            
            if (showDebugLogs)
            {
                Debug.Log($"📝 Chunk registrado: {chunk.coordinates} ({chunk.enemySpawnConfigs.Count} spawns)");
            }
        }
        
        /// <summary>
        /// Crea un chunk vacío.
        /// </summary>
        private ChunkData CreateEmptyChunk(Vector2Int coords)
        {
            return new ChunkData
            {
                coordinates = coords,
                chunkId = $"chunk_{coords.x}_{coords.y}",
                enemySpawnConfigs = new List<EnemySpawnConfig>()
            };
        }
        
        /// <summary>
        /// Obtiene un chunk por coordenadas.
        /// </summary>
        public ChunkData GetChunk(Vector2Int coords)
        {
            chunks.TryGetValue(coords, out var chunk);
            return chunk;
        }
        
        /// <summary>
        /// Fuerza recarga de todos los chunks activos.
        /// </summary>
        public void ReloadAllChunks()
        {
            var chunksToReload = new List<Vector2Int>(loadedChunks);
            
            foreach (var coords in chunksToReload)
            {
                UnloadChunk(coords);
            }
            
            UpdateLoadedChunks();
            
            Debug.Log($"🔄 Chunks recargados: {chunksToReload.Count}");
        }
        
        /// <summary>
        /// Limpia todos los chunks (descarga todo).
        /// </summary>
        public void ClearAllChunks()
        {
            var chunksToUnload = new List<Vector2Int>(loadedChunks);
            
            foreach (var coords in chunksToUnload)
            {
                UnloadChunk(coords);
            }
            
            Debug.Log($"🧹 Todos los chunks descargados: {chunksToUnload.Count}");
        }
        
        /// <summary>
        /// Notifica que un enemigo fue derrotado.
        /// Llamado desde EnemyController.ManejarMuerte().
        /// </summary>
        public void NotificarEnemigoDerrotado(string spawnId, Vector2Int chunkCoords, EnemyController controller)
        {
            if (!chunks.TryGetValue(chunkCoords, out var chunk))
            {
                Debug.LogWarning($"⚠️ Chunk {chunkCoords} no encontrado al notificar muerte de {spawnId}");
                return;
            }
            
            // Marcar como derrotado en esta sesión
            chunk.MarkEnemyDefeated(spawnId, isPermanent: false);
            
            // Remover de la lista de activos
            chunk.RemoveActiveEnemy(controller);
            
            if (showDebugLogs)
            {
                Debug.Log($"💀 Enemigo derrotado: {spawnId} en chunk {chunkCoords}");
            }
            
            // Devolver al pool después del delay de la animación
            StartCoroutine(DevolverControllerAlPoolConDelay(controller, 2.5f));
        }
        
        /// <summary>
        /// Devuelve un controller al pool después de un delay.
        /// </summary>
        private System.Collections.IEnumerator DevolverControllerAlPoolConDelay(EnemyController controller, float delay)
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
        
        /// <summary>
        /// Resetea el estado de todos los chunks (llamar al reiniciar el juego).
        /// </summary>
        public void ResetAllSessionState()
        {
            foreach (var chunk in chunks.Values)
            {
                chunk.ResetSessionState();
            }
            
            Debug.Log("🔄 Estado de sesión reseteado para todos los chunks");
        }
        
        void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            // Dibujar chunks cargados
            Gizmos.color = Color.green;
            foreach (var coords in loadedChunks)
            {
                Vector3 center = ChunkToWorldPos(coords);
                Gizmos.DrawWireCube(center, new Vector3(chunkSize, 1, chunkSize));
            }
            
            // Dibujar chunk del jugador
            if (playerTransform != null)
            {
                Gizmos.color = Color.yellow;
                var playerChunk = WorldToChunkCoords(playerTransform.position);
                Vector3 center = ChunkToWorldPos(playerChunk);
                Gizmos.DrawWireCube(center, new Vector3(chunkSize, 2, chunkSize));
            }
        }
        
        void OnDestroy()
        {
            ClearAllChunks();
        }
        
        #region Debug Methods
        
        [ContextMenu("Debug: Mostrar Estado")]
        private void DebugMostrarEstado()
        {
            Debug.Log("=== WORLD CHUNK MANAGER ===");
            Debug.Log($"Chunks totales: {TotalChunks}");
            Debug.Log($"Chunks cargados: {LoadedChunksCount}");
            Debug.Log($"Enemigos activos: {TotalActiveEnemies}");
            Debug.Log($"Chunk del jugador: {currentPlayerChunk}");
            Debug.Log("");
            
            foreach (var kvp in chunks)
            {
                var stats = kvp.Value.GetStats();
                Debug.Log($"📦 Chunk {stats.Coordinates} [{(stats.IsLoaded ? "LOADED" : "unloaded")}]");
                Debug.Log($"   Spawns: {stats.TotalSpawns} | Activos: {stats.ActiveEnemies}");
                Debug.Log($"   Únicos: {stats.UniqueEnemies} | Derrotados: {stats.DefeatedUniques}");
            }
        }
        
        [ContextMenu("Debug: Recargar Chunks")]
        private void DebugRecargarChunks()
        {
            ReloadAllChunks();
        }
        
        #endregion
    }
}
