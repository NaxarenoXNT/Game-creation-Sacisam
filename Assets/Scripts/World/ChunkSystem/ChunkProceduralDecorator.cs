using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using World.BiomeSystem;

namespace World.ChunkSystem
{
    /// <summary>
    /// Configuración para la generación procedural de decoración en chunks.
    /// Se pasa desde WorldChunkManager para evitar acoplar estado mutable.
    /// </summary>
    public struct ProceduralDecorationConfig
    {
        public float chunkSize;
        public int maxDecorativePropsPerChunk;
        public int proceduralPlacementAttempts;
        public int maxPropsPerFrame;
        public bool showDebugLogs;
    }

    /// <summary>
    /// Genera decoración procedural determinística en chunks basada en el sistema de biomas.
    /// Mismas coordenadas → misma semilla → mismos objetos siempre.
    /// Requiere WorldBiomeMap en la escena.
    /// </summary>
    public class ChunkProceduralDecorator
    {
        /// <summary>
        /// Coroutine que genera decoración procedural (vegetación, rocas, cobertura de suelo)
        /// distribuida entre frames para evitar spikes.
        /// </summary>
        public IEnumerator SpawnProceduralDecorationCoroutine(ChunkData chunk, ProceduralDecorationConfig config)
        {
            if (WorldBiomeMap.Instance == null)
            {
                if (config.showDebugLogs)
                    Debug.LogWarning("⚠️ WorldBiomeMap.Instance es null. No se genera decoración procedural.");
                yield break;
            }
            
            // Semilla determinística basada en coordenadas del chunk
            int seed = chunk.coordinates.x * 73856093 ^ chunk.coordinates.y * 19349663;
            var rng = new System.Random(seed);
            
            // Origen = esquina SW del chunk
            Vector3 origin = new Vector3(
                chunk.coordinates.x * config.chunkSize, 0f,
                chunk.coordinates.y * config.chunkSize);
            
            int placedCount = 0;
            int spawnedThisFrame = 0;

            // Lista plana de posiciones XZ ya colocadas — O(1) acceso, sin overhead de Transform.
            // Pre-alocar al máximo esperado para evitar re-allocations.
            var placedPositions = new List<Vector2>(config.maxDecorativePropsPerChunk);
            
            for (int attempt = 0; attempt < config.proceduralPlacementAttempts; attempt++)
            {
                if (placedCount >= config.maxDecorativePropsPerChunk) break;
                
                // Posición candidata aleatoria dentro del chunk
                float x = (float)rng.NextDouble() * config.chunkSize + origin.x;
                float z = (float)rng.NextDouble() * config.chunkSize + origin.z;
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
                if (placedCount < config.maxDecorativePropsPerChunk)
                {
                    float groundCoverDensity = sample.BlendFloat(b => b.groundCoverDensity);
                    if (rng.NextDouble() < groundCoverDensity)
                    {
                        float gcX = (float)rng.NextDouble() * config.chunkSize + origin.x;
                        float gcZ = (float)rng.NextDouble() * config.chunkSize + origin.z;
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
                if (placedCount < config.maxDecorativePropsPerChunk)
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
                                float gcX = (float)rng.NextDouble() * config.chunkSize + origin.x;
                                float gcZ = (float)rng.NextDouble() * config.chunkSize + origin.z;
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
                if (spawnedThisFrame >= config.maxPropsPerFrame)
                {
                    spawnedThisFrame = 0;
                    yield return null;
                    
                    // Verificar que el chunk siga cargado
                    if (!chunk.isLoaded) yield break;
                }
            }
            
            if (config.showDebugLogs && placedCount > 0)
                Debug.Log($"🌲 Chunk {chunk.coordinates}: {placedCount} props procedurales generados.");
        }
        
        /// <summary>
        /// Instancia un prop decorativo con variación de escala y rotación aleatoria.
        /// Mantiene los materiales originales del prefab sin modificarlos.
        /// </summary>
        private static void PlaceDecorativeProp(GameObject prefab, Vector3 position,
            BiomeSample sample, System.Random rng, Transform parent)
        {
            float scaleVar = sample.BlendFloat(b => b.treeScaleVariation);
            float scale = 1f + ((float)rng.NextDouble() * 2f - 1f) * scaleVar;
            float rotY = (float)rng.NextDouble() * 360f;
            
            Object.Instantiate(prefab, position, Quaternion.Euler(0, rotY, 0), parent)
                .transform.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// Instancia un prop tintado por bioma usando MaterialPropertyBlock.
        /// Escribe _TopColor = foliageColor blended sin crear instancias de material,
        /// preservando GPU Instancing. Activa _CUSTOMCOLORSTINTING = 1 automáticamente.
        /// </summary>
        private static void PlaceTintedProp(GameObject prefab, Vector3 position,
            BiomeSample sample, System.Random rng, Transform parent)
        {
            float scaleVar = sample.BlendFloat(b => b.treeScaleVariation);
            float scale = 1f + ((float)rng.NextDouble() * 2f - 1f) * scaleVar;
            float rotY = (float)rng.NextDouble() * 360f;

            var go = Object.Instantiate(prefab, position, Quaternion.Euler(0, rotY, 0), parent);
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
        /// Opera sobre una List&lt;Vector2&gt; de coordenadas XZ — sin overhead de Transform.
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
        private static float GetTerrainHeight(float x, float z)
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
    }
}
