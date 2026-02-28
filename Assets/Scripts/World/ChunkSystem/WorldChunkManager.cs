using System.Collections.Generic;
using UnityEngine;
using Managers;
using World.BiomeSystem;

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
        
        [Tooltip("Máximo de objetos decorativos (árboles, rocas) por chunk")]
        [SerializeField] private int maxDecorativePropsPerChunk = 150;
        
        [Tooltip("Cantidad de intentos para colocar un objeto procedural por chunk")]
        [SerializeField] private int proceduralPlacementAttempts = 400;
        
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
                Debug.Log($"📦 Cargando chunk {coords} ({chunkData.enemySpawnConfigs.Count} spawns, {chunkData.propSpawnConfigs.Count} props)");
            }
            
            // Crear contenedor de props para este chunk
            EnsurePropsRoot(chunkData);
            
            // Paso 1: Props con identidad (manuales)
            SpawnNamedProps(chunkData);
            
            // Paso 2: Decoración procedural (vegetación, rocas)
            StartCoroutine(SpawnProceduralDecorationCoroutine(chunkData));
            
            // Paso 3: Enemigos
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
            
            // Destruir props del chunk
            UnloadProps(chunkData);
            
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
        
        // ─── Decoración Procedural ─────────────────────────────────────────────────
        
        /// <summary>
        /// Genera decoración procedural determinística en el chunk.
        /// Mismas coordenadas → misma semilla → mismos objetos siempre.
        /// Requiere WorldBiomeMap en la escena. Si no existe, no genera nada.
        /// </summary>
        private System.Collections.IEnumerator SpawnProceduralDecorationCoroutine(ChunkData chunk)
        {
            if (WorldBiomeMap.Instance == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning("⚠️ WorldBiomeMap.Instance es null. No se genera decoración procedural.");
                yield break;
            }
            
            EnsurePropsRoot(chunk);
            
            // Semilla determinística basada en coordenadas del chunk
            int seed = chunk.coordinates.x * 73856093 ^ chunk.coordinates.y * 19349663;
            var rng = new System.Random(seed);
            
            // Origen = esquina SW del chunk
            Vector3 chunkCenter = ChunkToWorldPos(chunk.coordinates);
            Vector3 origin = chunkCenter - new Vector3(chunkSize * 0.5f, 0, chunkSize * 0.5f);
            
            int placedCount = 0;
            int spawnedThisFrame = 0;

            // Lista plana de posiciones XZ ya colocadas — O(1) acceso, sin overhead de Transform.
            // Pre-alocar al máximo esperado para evitar re-allocations.
            var placedPositions = new List<Vector2>(maxDecorativePropsPerChunk);
            
            for (int attempt = 0; attempt < proceduralPlacementAttempts; attempt++)
            {
                if (placedCount >= maxDecorativePropsPerChunk) break;
                
                // Posición candidata aleatoria dentro del chunk
                float x = (float)rng.NextDouble() * chunkSize + origin.x;
                float z = (float)rng.NextDouble() * chunkSize + origin.z;
                float y = GetTerrainHeight(x, z);
                Vector3 position = new Vector3(x, y, z);
                
                // Verificar exclusiones del chunk
                if (chunk.IsInExclusionZone(position)) continue;
                
                // Samplear bioma en esta posición exacta
                var sample = WorldBiomeMap.Instance.GetBiomeAt(position);
                
                // Si la zona es completamente manual (ciudad, dungeon), no generar
                if (sample.IsFullyManual()) continue;
                
                // Intentar colocar árbol
                float treeDensity = sample.BlendFloat(b => b.treeDensity);
                if (rng.NextDouble() < treeDensity)
                {
                    var prefab = sample.PickTree(rng);
                    if (prefab != null)
                    {
                        float minSpacing = sample.BlendFloat(b => b.minTreeSpacing);
                        if (!IsTooClose(position, minSpacing, placedPositions))
                        {
                            PlaceDecorativeProp(prefab, position, sample, rng, chunk.propsRoot);
                            placedPositions.Add(new Vector2(position.x, position.z));
                            placedCount++;
                            spawnedThisFrame++;
                        }
                    }
                }
                else
                {
                    // Intentar colocar roca
                    float rockDensity = sample.BlendFloat(b => b.rockDensity);
                    if (rng.NextDouble() < rockDensity)
                    {
                        var prefab = sample.PickRock(rng);
                        if (prefab != null)
                        {
                            float minSpacing = sample.BlendFloat(b => b.minRockSpacing);
                            if (!IsTooClose(position, minSpacing, placedPositions))
                            {
                                PlaceDecorativeProp(prefab, position, sample, rng, chunk.propsRoot);
                                placedPositions.Add(new Vector2(position.x, position.z));
                                placedCount++;
                                spawnedThisFrame++;
                            }
                        }
                    }
                    // Intentar colocar sotobosque (arbustos, helechos, hongos)
                    else
                    {
                        float understoryDensity = sample.BlendFloat(b => b.understoryDensity);
                        if (rng.NextDouble() < understoryDensity)
                        {
                            var prefab = sample.PickUnderstory(rng);
                            if (prefab != null)
                            {
                                float minSpacing = sample.BlendFloat(b => b.minUnderstorySpacing);
                                if (!IsTooClose(position, minSpacing, placedPositions))
                                {
                                    PlaceDecorativeProp(prefab, position, sample, rng, chunk.propsRoot);
                                    placedPositions.Add(new Vector2(position.x, position.z));
                                    placedCount++;
                                    spawnedThisFrame++;
                                }
                            }
                        }
                    }
                }

                // Cobertura de suelo (pasto, musgo, hojas) — NO necesita spacing, puede solaparse
                if (placedCount < maxDecorativePropsPerChunk)
                {
                    float groundCoverDensity = sample.BlendFloat(b => b.groundCoverDensity);
                    if (rng.NextDouble() < groundCoverDensity)
                    {
                        float gcX = (float)rng.NextDouble() * chunkSize + origin.x;
                        float gcZ = (float)rng.NextDouble() * chunkSize + origin.z;
                        Vector3 gcPos = new Vector3(gcX, GetTerrainHeight(gcX, gcZ), gcZ);

                        if (!chunk.IsInExclusionZone(gcPos) && !sample.IsFullyManual())
                        {
                            var prefab = sample.PickGroundCover(rng);
                            if (prefab != null)
                            {
                                PlaceDecorativeProp(prefab, gcPos, sample, rng, chunk.propsRoot);
                                // Cobertura de suelo no se agrega a placedPositions: no bloquea otros objetos
                                placedCount++;
                                spawnedThisFrame++;
                            }
                        }
                    }
                }

                // Props tintados por bioma (biomeTintedTrees / Understory / GroundCover)
                // Se instancian con MaterialPropertyBlock (_TopColor) = foliageColor del bioma.
                if (placedCount < maxDecorativePropsPerChunk)
                {
                    float tintedTreeDensity = sample.BlendFloat(b => b.tintedTreeDensity);
                    if (rng.NextDouble() < tintedTreeDensity)
                    {
                        var prefab = sample.PickTintedTree(rng);
                        if (prefab != null)
                        {
                            float minSpacing = sample.BlendFloat(b => b.minTreeSpacing);
                            if (!IsTooClose(position, minSpacing, placedPositions))
                            {
                                PlaceTintedProp(prefab, position, sample, rng, chunk.propsRoot);
                                placedPositions.Add(new Vector2(position.x, position.z));
                                placedCount++;
                                spawnedThisFrame++;
                            }
                        }
                    }
                    else
                    {
                        float tintedUnderstoryDensity = sample.BlendFloat(b => b.tintedUnderstoryDensity);
                        if (rng.NextDouble() < tintedUnderstoryDensity)
                        {
                            var prefab = sample.PickTintedUnderstory(rng);
                            if (prefab != null)
                            {
                                float minSpacing = sample.BlendFloat(b => b.minUnderstorySpacing);
                                if (!IsTooClose(position, minSpacing, placedPositions))
                                {
                                    PlaceTintedProp(prefab, position, sample, rng, chunk.propsRoot);
                                    placedPositions.Add(new Vector2(position.x, position.z));
                                    placedCount++;
                                    spawnedThisFrame++;
                                }
                            }
                        }
                        else
                        {
                            float tintedGroundCoverDensity = sample.BlendFloat(b => b.tintedGroundCoverDensity);
                            if (rng.NextDouble() < tintedGroundCoverDensity)
                            {
                                float gcX = (float)rng.NextDouble() * chunkSize + origin.x;
                                float gcZ = (float)rng.NextDouble() * chunkSize + origin.z;
                                Vector3 gcPos = new Vector3(gcX, GetTerrainHeight(gcX, gcZ), gcZ);
                                if (!chunk.IsInExclusionZone(gcPos) && !sample.IsFullyManual())
                                {
                                    var prefab = sample.PickTintedGroundCover(rng);
                                    if (prefab != null)
                                    {
                                        PlaceTintedProp(prefab, gcPos, sample, rng, chunk.propsRoot);
                                        placedCount++;
                                        spawnedThisFrame++;
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Distribuir la carga entre frames
                if (spawnedThisFrame >= maxPropsPerFrame)
                {
                    spawnedThisFrame = 0;
                    yield return null;
                    
                    // Verificar que el chunk siga cargado
                    if (!chunk.isLoaded) yield break;
                }
            }
            
            if (showDebugLogs && placedCount > 0)
                Debug.Log($"🌲 Chunk {chunk.coordinates}: {placedCount} props procedurales generados.");
        }
        
        /// <summary>
        /// Instancia un prop decorativo con variación de escala y rotación aleatoria.
        /// Mantiene los materiales originales del prefab sin modificarlos.
        /// </summary>
        private void PlaceDecorativeProp(GameObject prefab, Vector3 position,
            BiomeSample sample, System.Random rng, Transform parent)
        {
            float scaleVar = sample.BlendFloat(b => b.treeScaleVariation);
            float scale = 1f + ((float)rng.NextDouble() * 2f - 1f) * scaleVar;
            float rotY = (float)rng.NextDouble() * 360f;
            
            Instantiate(prefab, position, Quaternion.Euler(0, rotY, 0), parent)
                .transform.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// Instancia un prop tintado por bioma usando MaterialPropertyBlock.
        /// Escribe _TopColor = foliageColor blended sin crear instancias de material,
        /// preservando GPU Instancing. Activa _CUSTOMCOLORSTINTING = 1 automáticamente.
        /// </summary>
        private void PlaceTintedProp(GameObject prefab, Vector3 position,
            BiomeSample sample, System.Random rng, Transform parent)
        {
            float scaleVar = sample.BlendFloat(b => b.treeScaleVariation);
            float scale = 1f + ((float)rng.NextDouble() * 2f - 1f) * scaleVar;
            float rotY = (float)rng.NextDouble() * 360f;

            var go = Instantiate(prefab, position, Quaternion.Euler(0, rotY, 0), parent);
            go.transform.localScale = Vector3.one * scale;

            // Aplicar color del bioma vía MPB a todos los Renderers del prefab
            Color foliageColor = sample.BlendColor(b => b.foliageColor);
            var mpb = new MaterialPropertyBlock();
            foreach (var rend in go.GetComponentsInChildren<Renderer>())
            {
                rend.GetPropertyBlock(mpb);
                mpb.SetColor("_TopColor", foliageColor);
                mpb.SetFloat("_CUSTOMCOLORSTINTING", 1f);
                rend.SetPropertyBlock(mpb);
            }
        }
        
        /// <summary>
        /// Devuelve true si alguna posición ya colocada está dentro de minDistance.
        /// Opera sobre una List<Vector2> de coordenadas XZ — sin overhead de Transform.
        /// Usar como: if (!IsTooClose(pos, spacing, placedPositions)) → colocar objeto.
        /// </summary>
        private static bool IsTooClose(Vector3 position, float minDistance, List<Vector2> placedPositions)
        {
            if (minDistance <= 0f) return false;

            float minDistSq = minDistance * minDistance;
            float px = position.x;
            float pz = position.z;

            for (int i = 0; i < placedPositions.Count; i++)
            {
                float dx = placedPositions[i].x - px;
                float dz = placedPositions[i].y - pz;
                if ((dx * dx + dz * dz) < minDistSq)
                    return true;
            }

            return false;
        }
        
        /// <summary>
        /// Devuelve la altura del terreno en una posición XZ.
        /// Soporta múltiples Terrain tiles: busca el tile que contiene el punto.
        /// </summary>
        private float GetTerrainHeight(float x, float z)
        {
            Vector3 worldPos = new Vector3(x, 0f, z);

            foreach (var terrain in Terrain.activeTerrains)
            {
                var td = terrain.terrainData;
                Vector3 tp = terrain.transform.position;

                if (x >= tp.x && x <= tp.x + td.size.x &&
                    z >= tp.z && z <= tp.z + td.size.z)
                {
                    return terrain.SampleHeight(worldPos);
                }
            }

            return 0f;
        }
        
        // ─── Props con Identidad ──────────────────────────────────────────────────
        
        /// <summary>
        /// Crea el GameObject contenedor de props para un chunk si no existe.
        /// </summary>
        private void EnsurePropsRoot(ChunkData chunk)
        {
            if (chunk.propsRoot != null) return;
            
            var root = new GameObject($"Props_{chunk.coordinates.x}_{chunk.coordinates.y}");
            chunk.propsRoot = root.transform;
        }
        
        /// <summary>
        /// Instancia los props con identidad del chunk (edificios, cofres, NPCs, etc.).
        /// </summary>
        private void SpawnNamedProps(ChunkData chunk)
        {
            var spawnableProps = chunk.GetSpawnableProps();
            if (spawnableProps.Count == 0) return;
            
            foreach (var config in spawnableProps)
            {
                if (config.propData == null)
                {
                    Debug.LogWarning($"⚠️ PropSpawnConfig '{config.propId}' no tiene PropData asignado.");
                    continue;
                }
                
                if (config.propData.prefab == null)
                {
                    Debug.LogWarning($"⚠️ PropData '{config.propData.propName}' no tiene prefab asignado.");
                    continue;
                }
                
                var go = Object.Instantiate(
                    config.propData.prefab,
                    config.position,
                    config.rotation,
                    chunk.propsRoot
                );
                go.transform.localScale = config.scale;
                
                // Si es interactivo, inicializar el PropController
                if (config.propData.isInteractive)
                {
                    var controller = go.GetComponent<PropController>();
                    if (controller != null)
                    {
                        controller.Initialize(config, chunk.coordinates);
                        config.activeController = controller;
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Prop '{config.propId}' marcado como interactivo pero " +
                                         $"el prefab no tiene PropController.");
                    }
                }
            }
            
            if (showDebugLogs)
                Debug.Log($"🏠 Chunk {chunk.coordinates}: {spawnableProps.Count} props instanciados.");
        }
        
        /// <summary>
        /// Destruye todos los props del chunk de una sola vez.
        /// </summary>
        private void UnloadProps(ChunkData chunk)
        {
            if (chunk.propsRoot != null)
            {
                Destroy(chunk.propsRoot.gameObject);
                chunk.propsRoot = null;
            }
            
            // Limpiar referencias de controladores en los configs
            foreach (var config in chunk.propSpawnConfigs)
                config.activeController = null;
        }
        
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
        
        // ─── Notificaciones de Enemigos ───────────────────────────────────────────
        
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
