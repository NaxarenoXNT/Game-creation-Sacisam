using System.Collections;
using System.Collections.Generic;
using GameFlow;
using UnityEngine;
using Managers;

namespace World.ChunkSystem
{
    /// <summary>
    /// Manager central del sistema de chunks.
    /// Orquesta carga/descarga de chunks delegando a sub-módulos especializados:
    /// - ChunkTerrainLoader: terreno dinámico
    /// - ChunkEnemySpawner: spawning/despawning de enemigos
    /// - ChunkProceduralDecorator: decoración procedural por bioma
    /// - ChunkPropsManager: props con identidad (edificios, cofres, NPCs)
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
        
        [Tooltip("Radio de carga en chunks (1 = solo chunk actual, 2 = chunk + vecinos, etc.)\n" +
                 "Con chunkSize=256: radio 2 = 5x5 chunks visibles (~1280u), radio 3 = 7x7 (~1792u)")]
        [SerializeField] private int loadRadius = 2;
        
        [Tooltip("Tiempo entre checks de carga/descarga (segundos)")]
        [SerializeField] private float updateInterval = 1f;
        
        [Header("Optimización")]
        [Tooltip("Tiempo mínimo antes de poder recargar un chunk (segundos)")]
        [SerializeField] private float minReloadTime = 5f;
        
        [Tooltip("Máximo de enemigos a spawnear por frame")]
        [SerializeField] private int maxSpawnsPerFrame = 5;
        
        [Header("Props y Decoración")]
        [Tooltip("Máximo de props con identidad a instanciar por frame")]
        [SerializeField] private int maxPropsPerFrame = 10;
        
        [Header("Terreno Dinámico")]
        [Tooltip("Si está activo, los TerrainData se instancian en runtime según distancia del player.\n" +
                 "Requiere que los TerrainData estén en Resources/World/TerrainData/.")]
        [SerializeField] private bool loadTerrainDynamically = true;
        
        [Tooltip("Máximo de objetos decorativos (árboles, rocas) por chunk")]
        [SerializeField] private int maxDecorativePropsPerChunk = 150;
        
        [Tooltip("Cantidad de intentos para colocar un objeto procedural por chunk")]
        [SerializeField] private int proceduralPlacementAttempts = 400;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool showDebugLogs = true;
        
        // Sub-módulos
        private ChunkTerrainLoader terrainLoader;
        private ChunkEnemySpawner enemySpawner;
        private ChunkProceduralDecorator proceduralDecorator;
        private ChunkPropsManager propsManager;
        
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

            // Suscribir antes de Start para no perder el evento si el GameFlow ya está activo
            EventBus.Suscribir<EventoGameFlowChanged>(OnGameFlowChanged);
        }
        
        IEnumerator Start()
        {
            // Esperar un frame para que todos los Awake/Start se hayan ejecutado (PlayerPartyManager, etc.)
            yield return null;

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
            
            // Inicializar sub-módulos
            terrainLoader = new ChunkTerrainLoader();
            enemySpawner = new ChunkEnemySpawner(enemyPoolManager, WorldToChunkCoords);
            proceduralDecorator = new ChunkProceduralDecorator();
            propsManager = new ChunkPropsManager();
            
            // Auto-cargar ChunkDataAssets desde Resources/World/Chunks/
            AutoLoadChunkAssets();
            
            if (showDebugLogs)
            {
                Debug.Log($"✨ WorldChunkManager inicializado | ChunkSize: {chunkSize} | LoadRadius: {loadRadius}");
            }
            
            // Cargar chunks iniciales desde la posición actual del jugador
            if (playerTransform != null)
            {
                currentPlayerChunk = WorldToChunkCoords(playerTransform.position);
                lastPlayerChunk = currentPlayerChunk;
                UpdateLoadedChunks();
                
                if (showDebugLogs)
                {
                    Debug.Log($"🗺️ Chunks iniciales cargados. Player en chunk: {currentPlayerChunk}");
                }
            }
            else
            {
                Debug.LogWarning("[WorldChunkManager] playerTransform es null tras el primer frame. " +
                                 "Se intentará resolver en Update(). " +
                                 "Verifica que el campo playerTransform en el Inspector esté asignado " +
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
            
            // Detectar coordenadas duplicadas antes de registrar
            var seenCoords = new Dictionary<Vector2Int, string>();

            foreach (var asset in chunkAssets)
            {
                if (asset == null) continue;

                var runtimeData = asset.ToRuntimeData();

                if (seenCoords.TryGetValue(runtimeData.coordinates, out string firstName))
                {
                    Debug.LogError(
                        $"❌ ChunkDataAsset duplicado en coordenadas {runtimeData.coordinates}: " +
                        $"'{firstName}' y '{asset.name}'. Solo se usará el primero. " +
                        $"Revisá Resources/World/Chunks/");
                    continue;
                }

                seenCoords[runtimeData.coordinates] = asset.name;

                if (!chunks.ContainsKey(runtimeData.coordinates))
                {
                    chunks[runtimeData.coordinates] = runtimeData;
                    loaded++;

                    if (runtimeData.enemySpawnConfigs.Count > 0)
                        withEnemies++;
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
        
        // ─── Ciclo de Vida de Chunks ──────────────────────────────────────────────
        
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
        /// Carga un chunk: terreno, props, decoración y enemigos.
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
                Debug.Log($"📦 Cargando chunk {coords} ({chunkData.enemySpawnConfigs.Count} spawns, {chunkData.propSpawnConfigs.Count} props)");
            }
            
            // Paso 0: Terreno dinámico
            if (loadTerrainDynamically)
                terrainLoader.LoadTerrainForChunk(chunkData, chunkSize, showDebugLogs);
            
            // Crear contenedor de props para este chunk
            propsManager.EnsurePropsRoot(chunkData);
            
            // Paso 1: Props con identidad (manuales)
            propsManager.SpawnNamedProps(chunkData, showDebugLogs);
            
            // Paso 2: Decoración procedural (vegetación, rocas)
            var decorConfig = new ProceduralDecorationConfig
            {
                chunkSize = chunkSize,
                maxDecorativePropsPerChunk = maxDecorativePropsPerChunk,
                proceduralPlacementAttempts = proceduralPlacementAttempts,
                maxPropsPerFrame = maxPropsPerFrame,
                showDebugLogs = showDebugLogs
            };
            StartCoroutine(proceduralDecorator.SpawnProceduralDecorationCoroutine(chunkData, decorConfig));
            
            // Paso 3: Enemigos
            StartCoroutine(enemySpawner.SpawnEnemiesCoroutine(chunkData, maxSpawnsPerFrame, showDebugLogs));
            
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
            
            // Destruir terreno del chunk
            if (loadTerrainDynamically)
                terrainLoader.UnloadTerrainForChunk(chunkData, showDebugLogs);
            
            // Destruir props del chunk
            propsManager.UnloadProps(chunkData);
            
            // Devolver enemigos al pool (solo los que están vivos)
            enemySpawner.ReturnEnemiesToPool(chunkData);
            
            chunkData.ClearActiveReferences();
            chunkData.isLoaded = false;
            chunkData.lastUnloadTime = Time.time;
            loadedChunks.Remove(coords);
        }
        
        // ─── Coordenadas ──────────────────────────────────────────────────────────
        
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
        
        // ─── Registro de Chunks ───────────────────────────────────────────────────
        
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
        /// Marca un enemigo único como derrotado buscando en todos los chunks.
        /// Útil para bosses identificados por uniqueId.
        /// </summary>
        public bool MarcarUnicoDerrotado(string uniqueId)
        {
            foreach (var chunk in chunks.Values)
            {
                var config = chunk.enemySpawnConfigs.Find(c => c.uniqueId == uniqueId);
                if (config != null)
                {
                    config.isDefeated = true;
                    if (showDebugLogs)
                        Debug.Log($"✅ Único '{uniqueId}' marcado como derrotado en chunk {chunk.coordinates}");
                    return true;
                }
            }
            
            Debug.LogWarning($"⚠️ Enemigo único '{uniqueId}' no encontrado en ningún chunk");
            return false;
        }
        
        // ─── Gestión Global ───────────────────────────────────────────────────────
        
        /// <summary>
        /// Fuerza la actualización del sistema de chunks como si el jugador estuviera en
        /// <paramref name="position"/>. Útil tras un teleport para cargar inmediatamente
        /// los chunks del destino sin esperar al siguiente ciclo de Update.
        /// </summary>
        public void ForceUpdateAtPosition(Vector3 position)
        {
            currentPlayerChunk = WorldToChunkCoords(position);
            lastPlayerChunk    = currentPlayerChunk;
            UpdateLoadedChunks();

            if (showDebugLogs)
                Debug.Log($"[WorldChunkManager] ForceUpdate desde posición {position} → chunk {currentPlayerChunk}");
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
        
        // ─── Notificaciones ───────────────────────────────────────────────────────
        
        /// <summary>
        /// Notifica que un prop fue consumido (el jugador interactuó y desapareció).
        /// Llamado desde PropController.ConsumeObject().
        /// </summary>
        public void NotificarPropConsumido(string propId, Vector2Int chunkCoords)
        {
            if (!chunks.TryGetValue(chunkCoords, out var chunk))
            {
                Debug.LogWarning($"⚠️ Chunk {chunkCoords} no encontrado al notificar consumo de '{propId}'");
                return;
            }
            
            chunk.MarkPropConsumed(propId);
            
            if (showDebugLogs)
                Debug.Log($"📦 Prop consumido: {propId} en chunk {chunkCoords}");
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
            StartCoroutine(enemySpawner.ReturnControllerToPoolCoroutine(controller, 2.5f, showDebugLogs));
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
        
        // ─── Debug ────────────────────────────────────────────────────────────────
        
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
            EventBus.Desuscribir<EventoGameFlowChanged>(OnGameFlowChanged);
            ClearAllChunks();
        }

        /// <summary>
        /// Re-inicializa la carga de chunks al entrar en Exploration.
        /// Garantiza que el jugador está en su posición real del mundo (no en (0,0,0)).
        /// </summary>
        private void OnGameFlowChanged(EventoGameFlowChanged evt)
        {
            if (!(evt.NuevoEstado is ExplorationFlowState)) return;

            // Resolver playerTransform si aún no está asignado
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
            }

            if (playerTransform == null) return;

            // Forzar recarga desde la posición actual del jugador
            currentPlayerChunk = WorldToChunkCoords(playerTransform.position);
            lastPlayerChunk = currentPlayerChunk;
            UpdateLoadedChunks();

            if (showDebugLogs)
                Debug.Log($"[WorldChunkManager] Carga inicial forzada al entrar en Exploration. Chunk: {currentPlayerChunk}");
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
