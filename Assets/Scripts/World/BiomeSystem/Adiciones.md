// ============================================================
// INSTRUCCIONES: Este archivo NO reemplaza tu ChunkData.cs
// existente. Contiene los campos y métodos que tenés que
// AGREGAR a tu clase ChunkData y ChunkDataAsset actuales.
//
// ASUNCIONES sobre tu ChunkData actual (deduzco del WorldChunkManager):
//   - Tiene: coordinates, chunkId, isLoaded
//   - Tiene: lastLoadTime, lastUnloadTime
//   - Tiene: enemySpawnConfigs (List<EnemySpawnConfig>)
//   - Tiene: activeEnemies (List<EnemyController>)
//   - Tiene: GetSpawnableConfigs(), MarkEnemyDefeated(), RemoveActiveEnemy()
//   - Tiene: ClearActiveReferences(), ResetSessionState(), GetStats()
//
// Si algún campo ya existe con otro nombre, ajustá las referencias.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace World.ChunkSystem
{
    // ─── AGREGAR a ChunkData ─────────────────────────────────────────────────────
    //
    // Dentro de tu clase ChunkData existente, agregar estos campos y métodos:
    //
    // public partial class ChunkData   ← o simplemente abrís tu clase y agregás

    /*
    // ── Campos nuevos ──────────────────────────────────────────────────────────

    /// <summary>Props con identidad: edificios, cofres, NPCs. Configurados manualmente.</summary>
    public List<PropSpawnConfig> propSpawnConfigs = new List<PropSpawnConfig>();

    /// <summary>Zonas donde la generación procedural no coloca nada.</summary>
    public List<ProceduralExclusion> proceduralExclusions = new List<ProceduralExclusion>();

    /// <summary>
    /// Referencia al GameObject padre de toda la decoración procedural de este chunk.
    /// Se crea al cargar y se destruye al descargar.
    /// </summary>
    [System.NonSerialized]
    public Transform propsRoot;

    // ── Métodos nuevos ─────────────────────────────────────────────────────────

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
    /// Marca un prop como consumido. Llamado desde WorldChunkManager.NotificarPropConsumido().
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
    */

    // ─── AGREGAR a ChunkDataAsset ────────────────────────────────────────────────
    //
    // En tu ChunkDataAsset (el ScriptableObject que persiste en disco),
    // agregar estos campos para que se puedan editar en el Inspector:
    //
    // ASUNCIÓN: ChunkDataAsset tiene un método ToRuntimeData() que convierte
    // el asset a un ChunkData en runtime. Si es así, ese método también
    // debe copiar los nuevos campos.

    /*
    // En ChunkDataAsset, agregar:
    [Header("Props con Identidad")]
    [Tooltip("Objetos con posición fija: edificios, cofres, NPCs, entradas a zonas.")]
    public List<PropSpawnConfig> propSpawnConfigs = new List<PropSpawnConfig>();

    [Header("Exclusiones Procedurales")]
    [Tooltip("Zonas donde no se genera vegetación procedural: caminos, plazas, footprints de edificios.")]
    public List<ProceduralExclusion> proceduralExclusions = new List<ProceduralExclusion>();

    // En ToRuntimeData(), agregar al final antes del return:
    //   runtimeData.propSpawnConfigs = new List<PropSpawnConfig>(propSpawnConfigs);
    //   runtimeData.proceduralExclusions = new List<ProceduralExclusion>(proceduralExclusions);
    */


    // ─── CLASE COMPLETA si preferís copiar/pegar ─────────────────────────────────
    //
    // Si preferís ver los campos en contexto completo, acá están solo los nuevos
    // campos integrados con los que ya tenés (los que ya tenés están comentados
    // con "// YA EXISTE"):

    /// <summary>
    /// Ejemplo de cómo queda ChunkData con las adiciones.
    /// SOLO para referencia. No compilar esto, integrarlo con tu clase existente.
    /// </summary>
    internal class ChunkDataExample
    {
        // YA EXISTE: public Vector2Int coordinates;
        // YA EXISTE: public string chunkId;
        // YA EXISTE: public bool isLoaded;
        // YA EXISTE: public float lastLoadTime, lastUnloadTime;
        // YA EXISTE: public List<EnemySpawnConfig> enemySpawnConfigs;
        // YA EXISTE: public List<EnemyController> activeEnemies;

        // ── AGREGAR ──
        public List<PropSpawnConfig> propSpawnConfigs = new List<PropSpawnConfig>();
        public List<ProceduralExclusion> proceduralExclusions = new List<ProceduralExclusion>();

        [System.NonSerialized]
        public Transform propsRoot;

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
        }

        public bool IsInExclusionZone(Vector3 worldPos)
        {
            foreach (var exclusion in proceduralExclusions)
                if (exclusion.Contains(worldPos)) return true;
            return false;
        }
    }
}













// ============================================================
// INSTRUCCIONES: Este archivo contiene los métodos y campos
// que tenés que AGREGAR a tu WorldChunkManager.cs existente.
//
// NO reemplaza tu WorldChunkManager actual. Integrá cada
// sección en el lugar indicado.
//
// ASUNCIONES marcadas con ⚠️ donde necesitás verificar o ajustar.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using World.BiomeSystem;

namespace World.ChunkSystem
{
    public partial class WorldChunkManager : MonoBehaviour
    {
        // ════════════════════════════════════════════════════════════════════════
        // SECCIÓN 1: CAMPOS NUEVOS
        // Agregar en la región [Header] de tu WorldChunkManager existente.
        // ════════════════════════════════════════════════════════════════════════

        [Header("Props y Decoración Procedural")]
        [Tooltip("Máximo de objetos decorativos (árboles, rocas) por chunk. " +
                 "Ajustar según performance target.")]
        [SerializeField] private int maxDecorativePropsPerChunk = 150;

        [Tooltip("Cantidad de intentos para colocar un objeto procedural. " +
                 "Más intentos = más denso pero más costo al cargar.")]
        [SerializeField] private int proceduralPlacementAttempts = 400;

        [Tooltip("Máximo de props con identidad (manuales) a instanciar por frame. " +
                 "Análogo a maxSpawnsPerFrame para enemigos.")]
        [SerializeField] private int maxPropsPerFrame = 10;

        // ⚠️ ASUNCIÓN: WorldBiomeMap.Instance existe en la escena.
        // Si preferís inyección directa, agregar:
        // [SerializeField] private WorldBiomeMap biomeMap;
        // y reemplazar WorldBiomeMap.Instance por biomeMap en los métodos.


        // ════════════════════════════════════════════════════════════════════════
        // SECCIÓN 2: MODIFICACIONES A MÉTODOS EXISTENTES
        //
        // En tu LoadChunk() existente, AGREGAR estas líneas después de
        // "StartCoroutine(SpawnEnemiesCoroutine(chunkData))":
        //
        //   SpawnNamedProps(chunkData);
        //   StartCoroutine(SpawnProceduralDecorationCoroutine(chunkData));
        //
        // En tu UnloadChunk() existente, AGREGAR antes de
        // "chunkData.ClearActiveReferences()":
        //
        //   UnloadProps(chunkData);
        //
        // ════════════════════════════════════════════════════════════════════════


        // ════════════════════════════════════════════════════════════════════════
        // SECCIÓN 3: MÉTODOS NUEVOS
        // Agregar en tu WorldChunkManager existente, dentro de la clase.
        // ════════════════════════════════════════════════════════════════════════

        // ─── Props con Identidad (manual) ────────────────────────────────────────

        /// <summary>
        /// Instancia los props con identidad del chunk (edificios, cofres, NPCs, etc.).
        /// Estos se cargan de golpe, sin coroutine, porque son pocos.
        /// </summary>
        private void SpawnNamedProps(ChunkData chunk)
        {
            // Crear el contenedor padre si no existe aún
            if (chunk.propsRoot == null)
            {
                var root = new GameObject($"Props_{chunk.coordinates.x}_{chunk.coordinates.y}");
                chunk.propsRoot = root.transform;

                // ⚠️ ASUNCIÓN: Tenés un GameObject en la escena llamado "--- WORLD ENVIRONMENT ---"
                // Si no, quitá la línea de parent o cambiá el nombre.
                var worldEnv = GameObject.Find("--- WORLD ENVIRONMENT ---");
                if (worldEnv != null)
                    chunk.propsRoot.SetParent(worldEnv.transform);
            }

            var spawnableProps = chunk.GetSpawnableProps();

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

                var go = Instantiate(
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

            if (showDebugLogs && spawnableProps.Count > 0)
                Debug.Log($"🏠 Chunk {chunk.coordinates}: {spawnableProps.Count} props con identidad instanciados.");
        }

        // ─── Decoración Procedural ────────────────────────────────────────────────

        /// <summary>
        /// Genera la decoración procedural del chunk de forma escalonada.
        /// Usa las coordenadas del chunk como semilla para ser determinístico:
        /// el mismo chunk siempre genera los mismos objetos en las mismas posiciones.
        /// </summary>
        private IEnumerator SpawnProceduralDecorationCoroutine(ChunkData chunk)
        {
            // ⚠️ ASUNCIÓN: WorldBiomeMap existe en la escena.
            if (WorldBiomeMap.Instance == null)
            {
                Debug.LogWarning("⚠️ WorldBiomeMap.Instance es null. No se genera decoración procedural. " +
                                 "Asegurate de tener un GameObject con WorldBiomeMap en la escena.");
                yield break;
            }

            // Asegurar que el contenedor exista
            if (chunk.propsRoot == null)
            {
                var root = new GameObject($"Props_{chunk.coordinates.x}_{chunk.coordinates.y}");
                chunk.propsRoot = root.transform;

                var worldEnv = GameObject.Find("--- WORLD ENVIRONMENT ---");
                if (worldEnv != null)
                    chunk.propsRoot.SetParent(worldEnv.transform);
            }

            // Semilla determinística basada en coordenadas del chunk
            // Mismas coordenadas → misma semilla → mismos objetos siempre
            int seed = chunk.coordinates.x * 73856093 ^ chunk.coordinates.y * 19349663;
            var rng = new System.Random(seed);

            // Origen del chunk en coordenadas de mundo
            // ⚠️ ASUNCIÓN: ChunkToWorldPos devuelve la esquina SW del chunk.
            // Si devuelve el centro, ajustar el cálculo de origin.
            Vector3 chunkCenter = ChunkToWorldPos(chunk.coordinates);
            Vector3 origin = chunkCenter - new Vector3(chunkSize * 0.5f, 0, chunkSize * 0.5f);

            int placedCount = 0;
            int spawnedThisFrame = 0;

            for (int attempt = 0; attempt < proceduralPlacementAttempts; attempt++)
            {
                if (placedCount >= maxDecorativePropsPerChunk) break;

                // Posición candidata aleatoria dentro del chunk
                float x = (float)rng.NextDouble() * chunkSize + origin.x;
                float z = (float)rng.NextDouble() * chunkSize + origin.z;

                // ⚠️ ASUNCIÓN: Tenés un método GetTerrainHeight(x, z) que devuelve
                // la altura del terreno en esa posición.
                // Si usás Unity Terrain: Terrain.activeTerrain.SampleHeight(new Vector3(x,0,z))
                // Si no tenés terreno dinámico, podés usar 0f temporalmente.
                float y = GetTerrainHeight(x, z);
                Vector3 position = new Vector3(x, y, z);

                // Verificar exclusiones del chunk
                if (chunk.IsInExclusionZone(position)) continue;

                // Samplear bioma en esta posición exacta
                var sample = WorldBiomeMap.Instance.GetBiomeAt(position);

                // Si la zona es completamente manual (ciudad, dungeon), no generar nada
                if (sample.IsFullyManual()) continue;

                // Intentar colocar árbol
                float treeDensity = sample.BlendFloat(b => b.treeDensity);
                if (rng.NextDouble() < treeDensity)
                {
                    var prefab = sample.PickTree(rng);
                    if (prefab != null)
                    {
                        float minSpacing = sample.BlendFloat(b => b.minTreeSpacing);
                        if (!HasClearance(position, minSpacing, chunk.propsRoot))
                        {
                            PlaceDecorativeProp(prefab, position, sample, rng, chunk.propsRoot);
                            placedCount++;
                            spawnedThisFrame++;
                        }
                    }
                }
                // Intentar colocar roca (si no se puso árbol)
                else
                {
                    float rockDensity = sample.BlendFloat(b => b.rockDensity);
                    if (rng.NextDouble() < rockDensity)
                    {
                        var prefab = sample.PickRock(rng);
                        if (prefab != null)
                        {
                            float minSpacing = sample.BlendFloat(b => b.minRockSpacing);
                            if (!HasClearance(position, minSpacing, chunk.propsRoot))
                            {
                                PlaceDecorativeProp(prefab, position, sample, rng, chunk.propsRoot);
                                placedCount++;
                                spawnedThisFrame++;
                            }
                        }
                    }
                }

                // Distribuir la carga entre frames
                if (spawnedThisFrame >= maxPropsPerFrame)
                {
                    spawnedThisFrame = 0;
                    yield return null;

                    // Verificar que el chunk siga cargado (el jugador puede haberse ido)
                    if (!chunk.isLoaded) yield break;
                }
            }

            if (showDebugLogs)
                Debug.Log($"🌲 Chunk {chunk.coordinates}: {placedCount} props procedurales generados.");
        }

        /// <summary>
        /// Instancia un prop decorativo con variación de escala y rotación aleatoria.
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
        /// Verifica si hay suficiente espacio libre alrededor de una posición.
        /// Chequea contra los hijos ya existentes del contenedor.
        /// </summary>
        private bool HasClearance(Vector3 position, float minDistance, Transform parent)
        {
            if (parent == null || minDistance <= 0f) return false;

            float minDistSq = minDistance * minDistance;

            foreach (Transform child in parent)
            {
                float dx = child.position.x - position.x;
                float dz = child.position.z - position.z;
                if ((dx * dx + dz * dz) < minDistSq)
                    return true; // Demasiado cerca, no hay clearance
            }

            return false; // Hay espacio
        }

        // ─── Descarga de Props ────────────────────────────────────────────────────

        /// <summary>
        /// Destruye todos los props del chunk de una sola vez.
        /// Llamar desde UnloadChunk() antes de ClearActiveReferences().
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

        // ─── Notificación de consumo ──────────────────────────────────────────────

        /// <summary>
        /// Notifica que un prop fue consumido (el jugador interactuó con él y desapareció).
        /// Llamado desde PropController.ConsumeObject() cuando persistConsumedState = true.
        /// </summary>
        public void NotificarPropConsumido(string propId, Vector2Int chunkCoords)
        {
            if (!chunks.TryGetValue(chunkCoords, out var chunk))
            {
                Debug.LogWarning($"⚠️ Chunk {chunkCoords} no encontrado al notificar consumo de '{propId}'");
                return;
            }

            chunk.MarkPropConsumed(propId);

            // ⚠️ TODO: Cuando implementes el SaveManager, acá es donde persistís el estado.
            // Ejemplo:
            // SaveManager.Instance.MarkPropConsumed(propId);

            if (showDebugLogs)
                Debug.Log($"📦 Prop consumido: {propId} en chunk {chunkCoords}");
        }

        // ─── Stub de terreno ──────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve la altura del terreno en una posición XZ.
        ///
        /// ⚠️ IMPLEMENTAR según tu sistema de terreno:
        ///
        /// Opción A - Unity Terrain:
        ///   return Terrain.activeTerrain.SampleHeight(new Vector3(x, 0, z));
        ///
        /// Opción B - Heightmap propio:
        ///   return HeightmapManager.Instance.GetHeight(x, z);
        ///
        /// Opción C - Sin terreno dinámico (desarrollo):
        ///   return 0f;
        /// </summary>
        private float GetTerrainHeight(float x, float z)
        {
            // ⚠️ REEMPLAZAR con tu implementación real.
            // Temporalmente devuelve 0 para que compile.
            if (Terrain.activeTerrain != null)
                return Terrain.activeTerrain.SampleHeight(new Vector3(x, 0, z));
            return 0f;
        }
    }
}