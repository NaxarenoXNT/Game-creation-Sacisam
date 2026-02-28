# Sistema de Biomas y Props del Mundo

> **📚 Documentación relacionada:**
> - [24_ChunkSystem.md](24_ChunkSystem.md) — Sistema de chunks base
> - [25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md) — Integración con enemigos
> - [26_Sistema_Plantillas_Spawn.md](26_Sistema_Plantillas_Spawn.md) — Plantillas de spawn

> **✅ Estado:** Sistema completamente implementado en código. Pendiente: crear los assets `BiomeSettings` y prefabs en Unity.

---

## 📋 Resumen

Sistema híbrido para poblar el mundo con vegetación, estructuras y objetos interactivos. Combina **generación procedural determinística** para decoración densa (árboles, rocas, pasto) con **configuración manual** para objetos con identidad propia (edificios, cofres, NPCs).

**Principio central:** el bioma es un campo continuo en el mundo, no una etiqueta fija de chunk. Esto permite transiciones suaves entre biomas que cruzan múltiples chunks sin cortes abruptos.

---

## 🏗️ Arquitectura General

```
WorldBiomeMap (Singleton)                          ✅ implementado
├── Lista de BiomeControlPoints   ← definís a mano en el editor
├── GetBiomeAt(Vector3)           ← devuelve blend de biomas en cualquier punto
└── IsInExclusionZone(Vector3)    ← verifica zonas sin generación procedural

BiomeSettings (ScriptableObject, uno por bioma)    ✅ implementado
├── foliageColor               ← color del bioma para tintear props vía MPB
├── terrainLayer               ← TerrainLayer para splatmap del Terrain
├── biomeTintedTrees/Understory/GroundCover ← props que HEREDAN color del bioma
├── treeTypes/rockTypes/understory/groundCover ← props con color original (default)
└── Flags especiales (usesManualLayoutOnly para ciudades)

ChunkData (extendido)                              ✅ implementado
├── enemySpawnConfigs[]           ← ya existía
├── propSpawnConfigs[]            ← objetos con identidad (manual)
├── proceduralExclusions[]        ← zonas sin generación en este chunk
└── propsRoot (Transform)         ← contenedor runtime de props

WorldChunkManager (extendido)                      ✅ implementado
├── SpawnEnemies()                ← ya existía
├── SpawnNamedProps()             ← props con identidad
├── PlaceDecorativeProp()         ← instancia defaultProps (sin tocar materiales)
├── PlaceTintedProp()             ← instancia biomeTintedProps + MaterialPropertyBlock
└── SpawnProceduralDecoration()   ← vegetación/decoración procedural

WorldGeneratorPro (EditorWindow)                   ✅ implementado
└── Aplicar Splatmap por Biomas   ← pinta alphamaps según WorldBiomeMap + PerlinNoise pradera

WorldBiomeMapEditor (Custom Editor)                ✅ implementado
└── Scene View: esferas clickeables para mover/eliminar control points
```

---

## 🌍 El Sistema de Biomas como Campo Continuo

### El problema que resuelve

Si cada chunk tuviera un único `biomeType` fijo, la transición entre dos chunks de biomas distintos sería una línea recta abrupta. El jugador notaría exactamente dónde termina un chunk y empieza otro.

La solución es separar el concepto de **bioma** del concepto de **chunk**. Un bioma es una influencia que existe en coordenadas del mundo, y cualquier punto XZ del mundo tiene un blend de varios biomas con sus pesos.

### Cómo funciona

Se definen `BiomeControlPoints` en el mundo: posiciones donde un bioma domina. El sistema interpola entre ellos según la distancia. Cada punto del mundo tiene una muestra como esta:

```
Punto (320, 0, 180)  →  BosqueFragoso: 0.85 | BosqueNormal: 0.12 | Pradera: 0.03
Punto (800, 0, 400)  →  BosqueFragoso: 0.30 | BosqueNormal: 0.50 | Pradera: 0.20   ← zona de transición
Punto (1400, 0, 600) →  Pradera: 0.90       | BosqueNormal: 0.08 | BosqueCiudad: 0.02
```

El generador procedural usa esos pesos para decidir qué prefab colocar y con qué densidad, árbol por árbol. El resultado es que la transición ocurre gradualmente a lo largo de decenas o cientos de metros, cruzando múltiples chunks sin que ninguno tenga que "saber" su bioma de antemano.

---

## 📦 Estructuras de Datos

### BiomeControlPoint

```csharp
[System.Serializable]
public class BiomeControlPoint
{
    public string pointId;              // "bosque_norte_01"
    public Vector3 worldPosition;       // Posición en el mundo
    public BiomeSettings dominantBiome; // El bioma que domina acá
    [Range(0.1f, 1f)]
    public float influence = 1f;        // Intensidad del punto
}
```

### WorldBiomeMap

```csharp
public class WorldBiomeMap : MonoBehaviour
{
    public static WorldBiomeMap Instance { get; private set; }

    [Header("Definición del Mundo")]
    [SerializeField] private List<BiomeControlPoint> controlPoints;
    
    [Header("Blending")]
    [Tooltip("Radio de influencia de cada punto de control (en unidades)")]
    [SerializeField] private float blendRadius = 300f;
    [Tooltip("Mínimo de influencia para que un bioma aparezca en el blend")]
    [SerializeField] private float minInfluenceThreshold = 0.05f;

    /// <summary>
    /// Devuelve qué biomas influyen en este punto y con qué peso.
    /// La suma de pesos siempre es 1.
    /// </summary>
    public BiomeSample GetBiomeAt(Vector3 worldPos) { ... }
}

[System.Serializable]
public class BiomeSample
{
    // Lista ordenada de mayor a menor influencia
    public List<(BiomeSettings biome, float weight)> influences;
    
    // El bioma dominante (mayor peso)
    public BiomeSettings Dominant => influences[0].biome;
    
    // Blend de un parámetro float entre todos los biomas
    public float BlendFloat(System.Func<BiomeSettings, float> selector)
    {
        float result = 0f;
        foreach (var (biome, weight) in influences)
            result += selector(biome) * weight;
        return result;
    }
}
```

### BiomeSettings (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "World/Biome Settings")]
public class BiomeSettings : ScriptableObject
{
    [Header("Info")]
    public string biomeName;            // "Bosque Frondoso"
    public BiomeCategory category;      // Forest, Mountain, Plains, Urban, Dark
    
    [Header("Colores del Bioma")]
    [ColorUsage(false, true)]
    public Color foliageColor;          // Color que se aplica vía MPB (_TopColor) a biomeTintedProps
    
    [Header("Terreno (Splatmap)")]
    public TerrainLayer terrainLayer;   // Textura de suelo para el alphamap del Terrain
    
    [Header("Vegetación — Tinteo por Bioma (biomeTintedProps)")]
    // Props que HEREDAN el foliageColor del bioma vía MaterialPropertyBlock
    [Range(0f, 1f)] public float tintedTreeDensity = 0f;
    public List<WeightedPrefab> biomeTintedTrees;
    [Range(0f, 1f)] public float tintedUnderstoryDensity = 0.3f;
    public List<WeightedPrefab> biomeTintedUnderstory;
    [Range(0f, 1f)] public float tintedGroundCoverDensity = 0.5f;
    public List<WeightedPrefab> biomeTintedGroundCover;
    
    [Header("Vegetación — Sin Tinteo (defaultProps)")]
    // Props que MANTIENEN sus colores/materiales originales sin modificar
    [Range(0f, 1f)] public float treeDensity = 0.7f;
    public List<WeightedPrefab> treeTypes;
    public float minTreeSpacing = 4f;
    [Range(0f, 0.5f)] public float treeScaleVariation = 0.2f;
    
    [Header("Rocas y Suelo (sin tinteo)")]
    [Range(0f, 1f)] public float rockDensity = 0.15f;
    public List<WeightedPrefab> rockTypes;
    [Range(0f, 1f)] public float understoryDensity = 0.4f;
    public List<WeightedPrefab> understoryTypes;
    
    [Header("Cobertura de Suelo (sin tinteo)")]
    public List<WeightedPrefab> groundCoverTypes;
    [Range(0f, 1f)] public float groundCoverDensity = 0.6f;
    
    [Header("Atmósfera")]
    public GameObject ambientParticlesPrefab;
    
    [Header("Flags")]
    public bool usesManualLayoutOnly = false;
}
```

> **⚠️ Nota sobre _CUSTOMCOLORSTINTING:** Los shaders de Polytope Studio requieren que
> `_CUSTOMCOLORSTINTING = 1` en el material para que el tinteo funcione. El sistema
> activa esta propiedad automáticamente en `PlaceTintedProp()` al instanciar.

> **⚠️ MPB y GPU Instancing:** Se usa `MaterialPropertyBlock` para escribir `_TopColor`
> sin crear instancias de material. Esto preserva el GPU Instancing del shader.
```

### PropSpawnConfig (objetos con identidad)

```csharp
[System.Serializable]
public class PropSpawnConfig
{
    [Header("Identificación")]
    public string propId;               // "cabaña_chunk_3_7_01" (único global)
    public PropData propData;           // ScriptableObject del objeto
    
    [Header("Transform")]
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale = Vector3.one;
    
    [Header("Estado")]
    public bool isConsumed;             // Para objetos que desaparecen al interactuar
    
    [Header("Interacción")]
    public bool isInteractive;
    public string interactionType;      // "cofre", "npc", "puerta", "consumible"
}
```

### PropData (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "World/Prop Data")]
public class PropData : ScriptableObject
{
    [Header("Info")]
    public string propName;
    public PropCategory category;   // Decoration, Structure, Interactive, NPC

    [Header("Visual")]
    public GameObject prefab;

    [Header("Comportamiento")]
    public bool isInteractive;
    [Tooltip("Si true, el objeto desaparece cuando el jugador interactúa")]
    public bool consumeOnInteract;
    [Tooltip("Si true, persiste el estado 'consumido' entre cargas del chunk")]
    public bool persistConsumedState;
}
```

### ProceduralExclusion (zonas sin generación)

```csharp
[System.Serializable]
public class ProceduralExclusion
{
    public ExclusionShape shape;        // Circle, Rectangle, Path

    // Para Circle
    public Vector3 center;
    public float radius;

    // Para Rectangle
    public Vector3 rectCenter;
    public Vector3 rectSize;
    public float rectRotation;

    // Para Path (caminos)
    public List<Vector3> pathPoints;
    public float pathWidth = 6f;

    public bool Contains(Vector3 point) { ... }
}
```

---

## 🔄 Flujo de Carga de un Chunk

Cuando el `WorldChunkManager` carga un chunk, ejecuta tres pasos en orden:

```csharp
private void LoadChunk(Vector2Int coords)
{
    var chunk = GetOrCreateChunk(coords);
    
    // Paso 1: Crear contenedor de props para este chunk
    var propsRoot = new GameObject($"Props_{coords.x}_{coords.y}");
    chunk.propsRoot = propsRoot.transform;

    // Paso 2: Props con identidad (configurados manualmente)
    SpawnNamedProps(chunk);

    // Paso 3: Decoración procedural (generada algorítmicamente)
    SpawnProceduralDecoration(chunk);

    // Paso 4: Enemigos (ya existe)
    SpawnEnemies(chunk);
    
    chunk.isLoaded = true;
}
```

Al descargar el chunk, toda la decoración procedural se destruye junto con el contenedor:

```csharp
private void UnloadChunk(Vector2Int coords)
{
    var chunk = GetChunk(coords);
    
    // Destruye todos los props de una sola vez
    if (chunk.propsRoot != null)
        Destroy(chunk.propsRoot.gameObject);
    
    // Devuelve enemigos al pool (ya existe)
    ReturnEnemiesToPool(chunk);
    
    chunk.isLoaded = false;
}
```

---

## 🌲 Generación Procedural Determinística

La generación es **determinística**: el mismo chunk siempre produce exactamente los mismos objetos en las mismas posiciones. Se logra usando las coordenadas del chunk como semilla del RNG.

```csharp
private void SpawnProceduralDecoration(ChunkData chunk)
{
    // Semilla fija basada en coordenadas = siempre igual para este chunk
    var rng = new System.Random(chunk.coordinates.x * 73856 + chunk.coordinates.y * 19349);
    
    Vector3 origin = ChunkToWorldPos(chunk.coordinates) - new Vector3(chunkSize / 2, 0, chunkSize / 2);
    int attempts = maxProceduralAttemptsPerChunk;
    
    while (attempts-- > 0)
    {
        float x = (float)rng.NextDouble() * chunkSize + origin.x;
        float z = (float)rng.NextDouble() * chunkSize + origin.z;
        Vector3 position = new Vector3(x, GetTerrainHeight(x, z), z);
        
        // Verificar exclusiones (caminos, clearings, zonas urbanas)
        if (IsInExclusionZone(position, chunk)) continue;
        
        // Samplear bioma en esta posición exacta
        var sample = WorldBiomeMap.Instance.GetBiomeAt(position);
        
        // Si el bioma dominante no genera proceduralmente, saltar
        if (sample.Dominant.usesManualLayoutOnly) continue;
        
        // Densidad blended entre todos los biomas que influyen
        float treeDensity = sample.BlendFloat(b => b.treeDensity);
        
        if (rng.NextDouble() < treeDensity)
        {
            var prefab = PickWeightedPrefab(sample, b => b.treeTypes, rng);
            if (prefab == null) continue;
            
            // Verificar spacing mínimo con objetos ya colocados
            float minSpacing = sample.BlendFloat(b => b.minTreeSpacing);
            if (!HasSpaceAt(position, minSpacing, chunk)) continue;
            
            float scaleVar = sample.BlendFloat(b => b.treeScaleVariation);
            float scale = 1f + ((float)rng.NextDouble() * 2 - 1) * scaleVar;
            float rotation = (float)rng.NextDouble() * 360f;
            
            var go = Instantiate(prefab, position,
                Quaternion.Euler(0, rotation, 0), chunk.propsRoot);
            go.transform.localScale = Vector3.one * scale;
        }
        
        // Similar para rocas y understory...
    }
}
```

---

## 🏙️ Props con Identidad (Manual)

```csharp
private void SpawnNamedProps(ChunkData chunk)
{
    foreach (var config in chunk.propSpawnConfigs)
    {
        // Si el objeto fue consumido y persiste ese estado, no spawnear
        if (config.isConsumed && config.propData.persistConsumedState)
            continue;
        
        var go = Instantiate(config.propData.prefab,
            config.position, config.rotation, chunk.propsRoot);
        go.transform.localScale = config.scale;
        
        if (config.isInteractive)
        {
            var controller = go.GetComponent<PropController>();
            controller?.Initialize(config, chunk.coordinates);
        }
    }
}
```

### PropController (para objetos interactivos)

```csharp
public class PropController : MonoBehaviour, IInteractable
{
    private PropSpawnConfig config;
    private Vector2Int chunkCoords;
    
    public void Initialize(PropSpawnConfig config, Vector2Int chunkCoords)
    {
        this.config = config;
        this.chunkCoords = chunkCoords;
        
        if (config.isConsumed)
            HandleConsumedVisualState();
    }
    
    public void OnInteract()
    {
        // Lógica específica según interactionType
        switch (config.interactionType)
        {
            case "cofre":
                OpenChest();
                break;
            case "consumible":
                ConsumeObject();
                break;
            case "npc":
                TriggerDialogue();
                break;
        }
    }
    
    private void ConsumeObject()
    {
        config.isConsumed = true;
        
        if (config.propData.persistConsumedState)
            WorldChunkManager.Instance.NotificarPropConsumido(config.propId, chunkCoords);
        
        gameObject.SetActive(false);
    }
}
```

---

## 🚧 Zonas de Exclusión

Las exclusiones evitan que la generación procedural invada caminos, plazas, zonas urbanas o cualquier área que vos querés controlar manualmente.

Se definen **por chunk** en el `ChunkDataAsset`, pero también se pueden definir globalmente en el `WorldBiomeMap` para zonas grandes (como toda una ciudad).

### Tipos de exclusión

**Circle:** Para clearings, plazas, zonas alrededor de estructuras.
```
clearingAt: center(320, 0, 180), radius: 30
→ Área circular de 30m sin vegetación procedural
```

**Rectangle:** Para edificios, muros, zonas rectangulares.
```
buildingAt: center(400, 0, 200), size(20, 0, 15), rotation: 45°
→ Footprint del edificio sin vegetación
```

**Path:** Para caminos entre puntos.
```
pathFrom: [(256, 150), (300, 160), (400, 180)], width: 8m
→ Corredor de 8m de ancho a lo largo del camino
```

---

## 🗺️ Diseño del Mapa: Flujo de Trabajo

### Para zonas naturales (bosques, montañas, llanuras)

1. Colocás `BiomeControlPoints` en el `WorldBiomeMap` con el bioma dominante de cada región.
2. Ajustás `blendRadius` para controlar qué tan suaves son las transiciones.
3. Si un chunk tiene alguna estructura o camino, le agregás exclusiones.
4. El resto se genera solo.

### Para transiciones específicas (ej: bosque frondoso → ciudad)

```
BiomeControlPoint:  (0, 0)      → BosqueFragoso
BiomeControlPoint:  (1500, 0)   → BosqueNormal
BiomeControlPoint:  (3000, 0)   → Pradera
BiomeControlPoint:  (4500, 0)   → BosqueCiudad   ← bioma intermedio, baja densidad
BiomeControlPoint:  (5500, 0)   → Ciudad          → usesManualLayoutOnly = true
```

Con `blendRadius: 300`, cada transición ocupa ~600 unidades de mundo (~2-3 chunks a 256u). El jugador recorre esa zona gradualmente y la vegetación va cambiando árbol por árbol.

### Para zonas urbanas (pueblos, ciudades)

1. Los chunks de la zona urbana tienen `biomeType` → `CiudadBiome` (con `usesManualLayoutOnly: true`).
2. Todo el contenido se define manualmente en `propSpawnConfigs`: casas, NPCs, cofres, pozos.
3. Los chunks del borde de la ciudad usan `biomeBlend` para que la vegetación se diluya gradualmente.
4. Los caminos que conectan la ciudad con el exterior se definen como exclusiones `Path` en los chunks del bosque.

---

## 📊 Biomas Planificados

### Biomas Naturales

| Bioma | Categoría | treeDensity | rockDensity | Notas |
|-------|-----------|-------------|-------------|-------|
| Bosque Frondoso | Forest | 0.85 | 0.10 | Alta densidad, pinos/robles, sotobosque denso |
| Bosque Normal | Forest | 0.55 | 0.15 | Mixto, bioma de transición frecuente |
| Pradera | Plains | 0.05 | 0.05 | Pasto alto, flores silvestres |
| Montaña | Mountain | 0.10 | 0.80 | Rocas grandes, poca vegetación, entradas a cuevas |
| Desierto | Arid | 0.02 | 0.30 | Cactus, rocas áridas, dunas |
| Costa | Coastal | 0.10 | 0.20 | Arena, rocas costeras, vegetación de playa |

### Biomas Oscuros / Corrupción

| Bioma | Categoría | treeDensity | rockDensity | Notas |
|-------|-----------|-------------|-------------|-------|
| Bosque Pútrido | Dark | 0.65 | 0.10 | Árboles muertos, niebla permanente |
| Pradera Corrompida | Dark | 0.03 | 0.15 | Pasto marchito, tierra quemada |
| Transición Corrupta | Dark-Transition | variable | variable | Bioma de blend entre normal y corrupto |

> **Ver sección "Sistema de Corrupción"** para cómo se integra con el WorldBiomeMap.

### Biomas Urbanos

| Bioma | Categoría | treeDensity | rockDensity | Notas |
|-------|-----------|-------------|-------------|-------|
| Ciudad | Urban | 0.00 | 0.00 | `usesManualLayoutOnly: true` |
| Pueblo | Urban | 0.00 | 0.00 | `usesManualLayoutOnly: true` |
| Borde Urbano | Urban-Transition | 0.15 | 0.05 | Granjas, jardines, huertos |

> **Ver sección "Zonas Urbanas"** para cómo se diseña la integración con el entorno.

---

## ☠️ Sistema de Corrupción

Los biomas oscuros/putrefactos son **zonas fijas del mapa** con sus propios `BiomeControlPoints`. No se expanden dinámicamente, lo que simplifica el sistema y mantiene el diseño del mundo bajo control.

### Cómo funciona la transición de corrupción

La transición de un bosque normal hacia un bosque pútrido es el caso más crítico de blending, porque tiene que sentirse como una degradación gradual y no como un cambio de bioma normal.

Se resuelve con un **bioma de transición explícito**: `TransiciónCorrupta`. Este bioma intermedio tiene parámetros que mezclan lo vivo con lo muerto: algunos árboles normales, algunos podridos, el suelo empieza a oscurecerse.

```
BiomeControlPoint: (2000, 0)  → BosqueNormal       (blendRadius: 400)
BiomeControlPoint: (2600, 0)  → TransiciónCorrupta (blendRadius: 300)
BiomeControlPoint: (3200, 0)  → BosquePútrido      (blendRadius: 400)
```

El resultado visual es:
- **Zona 1800-2200:** 100% bosque normal
- **Zona 2200-2400:** bosque normal con primeros árboles enfermos apareciendo
- **Zona 2400-2800:** mezcla pareja, atmósfera ambigua
- **Zona 2800-3000:** predomina lo podrido, cada vez menos árboles vivos
- **Zona 3000+:** 100% bosque pútrido

### BiomeSettings para TransiciónCorrupta

```
TransiciónCorrupta.asset
├── treeTypes: [ÁrbolNormal(40%), ÁrbolEnfermo(40%), TroncoMuerto(20%)]
├── treeDensity: 0.60
├── groundCoverTypes: [PastoMarchito, TierraOscura, Hongos]
├── ambientParticlesPrefab: NieblaLeve
└── understoryTypes: [Arbusto Marchito, Raíces Expuestas]
```

---

## 🏙️ Zonas Urbanas: Integración con el Entorno

Las ciudades y pueblos no flotan en el vacío: tienen bordes que se integran gradualmente con el bioma que los rodea. Esto se logra con el bioma `BordeUrbano` como transición.

### Capas de una ciudad típica

```
[Bosque/Pradera]
      ↓  blendRadius: 250
[Borde Urbano]          ← granjas, huertos, árboles dispersos, cercas
      ↓  blendRadius: 150  
[Ciudad/Pueblo]         ← usesManualLayoutOnly: true, todo manual
```

El `BordeUrbano` tiene baja densidad de vegetación natural (los árboles del bosque se van diluyendo) y puede incluir props manuales como cercas, campos de cultivo y cabañas de campo.

### Pueblos pequeños dispersos

Para pueblos pequeños dentro de biomas naturales el approach es más simple: el pueblo es una zona de exclusión grande con `usesManualLayoutOnly: true`, rodeada directamente por el bioma natural. No hace falta el bioma de borde urbano si el pueblo es pequeño.

```
Chunk con pueblo pequeño:
├── biomeType: BosqueNormal           ← el bosque rodea el pueblo
├── propSpawnConfigs: [casas, NPCs]   ← el pueblo en sí
└── proceduralExclusions:
    └── Circle(center: pueblo, radius: 60m)  ← área sin árboles
```

---

## 🏔️ Montañas y Cuevas

### Montañas como bioma superficial

Las montañas son zonas elevadas transitables. El bioma `Montaña` genera rocas grandes, vegetación escasa y entradas a cuevas como props manuales con `propId` único.

La transición desde un bosque hacia una montaña suele pasar por un bioma intermedio de `Estribaciones` (falda de montaña): más rocas, árboles más espaciados y pequeños, menos sotobosque.

```
BosqueNormal → Estribaciones → Montaña → CumbreMontaña (sin vegetación, nieve)
```

### Cuevas como zonas separadas

Las cuevas son **escenas o zonas separadas**, no continuación del chunk de superficie. El jugador llega a la entrada de la cueva (un prop manual en el chunk de montaña) y la interacción carga la zona de cueva aparte.

Esto simplifica enormemente el sistema de chunks: no hay que manejar geometría subterránea ni chunks en 3D. La cueva tiene su propio sistema de chunks si es grande, o es una escena fija si es pequeña.

```
ChunkMontaña_5_3:
└── propSpawnConfigs:
    └── EntradaCueva_01 (PropData: EntradaCueva, isInteractive: true)
        └── interactionType: "carga_zona"
        └── targetZone: "CuevaNorte_01"
```

---

## 🌊 Zonas Costeras

Las costas son **transitables pero el agua es decorativa**: el jugador camina por la orilla, no navega. Esto simplifica el sistema porque no hay chunks de agua, solo el bioma costero que termina en el borde del agua.

### Transición tierra → costa

```
BosqueNormal → PradreraLitoral → Costa → [límite del mundo / agua decorativa]
```

El bioma `Costa` genera arena, rocas costeras y vegetación de playa (palmeras, juncos). La línea de agua es una barrera invisible o visual, no un bioma con chunks propios.

Si en el futuro se decide agregar navegación o islas, el sistema de `BiomeControlPoints` lo soporta sin cambios: simplemente se agregan puntos de control en las coordenadas de las islas.

---

## 🛠️ Herramienta de Editor: Colocación de BiomeControlPoints

Como el mapa se define mientras se desarrolla, la herramienta tiene que ser rápida de usar y no requerir conocer coordenadas de antemano.

### Flujo recomendado: Gizmos en Scene View

El `WorldBiomeMap` dibuja sus control points como esferas coloreadas en la Scene View. Para agregar, mover o eliminar puntos no hace falta tipear coordenadas: se usa un Custom Editor que permite interactuar directamente en la vista.

```
Seleccionás WorldBiomeMap en Hierarchy
→ En Scene View aparecen esferas de colores (una por bioma)
→ Click en esfera = seleccionar ese punto (muestra parámetros en Inspector)
→ Drag = mover el punto
→ Botón "+" en Inspector = agregar nuevo punto donde está el cursor
→ Delete = eliminar punto seleccionado
```

Adicionalmente, el Inspector muestra una lista scrolleable con todos los puntos para edición rápida de parámetros sin tener que clickear en la Scene View.

### Visualización del blending en tiempo real

Con el Custom Editor activo, al mover el cursor por la Scene View se puede mostrar un overlay del bioma dominante en cada posición, lo que permite ver cómo quedan las transiciones antes de generar nada.

---

```
Assets/
└── Resources/
    └── World/
        ├── Chunks/                             ← ya existe
        │   └── Chunk_X_Y_Data.asset
        │
        ├── Biomes/                             ← NUEVO
        │   ├── Natural/
        │   │   ├── BosqueFragoso.asset
        │   │   ├── BosqueNormal.asset
        │   │   ├── Pradera.asset
        │   │   ├── PraderaLitoral.asset
        │   │   ├── Montana.asset
        │   │   ├── Estribaciones.asset         ← transición bosque→montaña
        │   │   ├── CumbreMontana.asset
        │   │   ├── Costa.asset
        │   │   └── Desierto.asset
        │   ├── Dark/
        │   │   ├── BosquePutrido.asset
        │   │   ├── PraderaCorrempida.asset
        │   │   └── TransicionCorrupta.asset    ← bioma de blend
        │   ├── Urban/
        │   │   ├── Ciudad.asset
        │   │   ├── Pueblo.asset
        │   │   └── BordeUrbano.asset           ← transición ciudad→naturaleza
        │   └── Transition/
        │       └── (biomas de blend adicionales)
        │
        └── Props/                              ← NUEVO
            ├── Structures/
            │   ├── Cabaña.asset
            │   ├── Taberna.asset
            │   └── TorreGuardia.asset
            ├── Interactive/
            │   ├── CofreBasico.asset
            │   ├── ConsumibleHierba.asset
            │   └── EntradaCueva.asset          ← prop con carga de zona
            └── NPCs/
                └── MercaderEstatico.asset
```

---

## ⚙️ Optimización

### Por qué no se usa pooling para props decorativos

Los props decorativos (árboles, rocas) **no usan object pool** a diferencia de los enemigos. El motivo es que:

- Son objetos estáticos sin lógica de update
- Se destruyen en bloque al descargar el chunk (un solo `Destroy` del GameObject padre)
- El overhead del pool no vale para objetos que no tienen ciclo de vida corto

El pool tiene sentido para los enemigos porque aparecen y desaparecen frecuentemente con lógica compleja. Para props decorativos, instanciar/destruir con el chunk es más eficiente.

### Límites recomendados

```
maxProceduralAttemptsPerChunk: 500   ← intentos por chunk (no todos resultan en objeto)
maxDecorativePropsPerChunk:    200   ← techo de objetos decorativos por chunk
maxNamedPropsPerChunk:          50   ← techo de props con identidad (no debería llegar)
```

### LOD y Culling

Los props generados deben usar LOD Groups para manejar el nivel de detalle según distancia. El `propsRoot` GameObject puede tener un `LODGroup` padre que gestione todo el chunk como unidad.

---

## 🐛 Debugging

### Visualizar biomas en Scene View

```csharp
// En WorldBiomeMap, gizmos en OnDrawGizmos:
// - Esfera en cada BiomeControlPoint con color por bioma
// - Radio de influencia como wireframe sphere
// - Texto con el nombre del bioma dominante
```

### Context Menu en WorldBiomeMap

```
[ContextMenu("Debug: Samplear posición del jugador")]
→ Muestra en consola el blend de biomas donde está el jugador

[ContextMenu("Debug: Regenerar chunk actual")]
→ Fuerza recarga del chunk donde está el jugador
```

---

## ⚠️ Notas Importantes

- **El `WorldBiomeMap` debe estar en la escena** antes de que el `WorldChunkManager` intente cargar chunks.
- **Los `BiomeControlPoints` se definen en coordenadas del mundo**, no en coordenadas de chunk.
- **`blendRadius` impacta el rendimiento** del sampleo: valores muy altos (>1000) hacen costoso `GetBiomeAt()`. Para mundos grandes, cachear el sampleo por chunk.
- **La semilla procedural es determinística pero no igual en distintas plataformas** si se usa `System.Random`. Para garantizar paridad entre plataformas usar una implementación propia de LCG o similar.
- **Los props con identidad (`propSpawnConfigs`) persisten estado entre sesiones** si `persistConsumedState` está activo. Requiere integración con el sistema de guardado cuando esté implementado.

---

## 🔮 Próximos Pasos

### ✅ Código implementado (todas las clases y sistemas)
1. ✅ `WorldBiomeMap` con interpolación cuadrática entre control points
2. ✅ `ChunkData` y `ChunkDataAsset` extendidos con `propSpawnConfigs` y `proceduralExclusions`
3. ✅ `SpawnProceduralDecoration()` integrado en `WorldChunkManager.LoadChunk()`
4. ✅ Editor visual (`WorldBiomeMapEditor`) para colocar y mover control points en Scene View
5. ✅ `PropController` con handlers para cofre, NPC, puerta, consumible, carga_zona
6. ✅ `BiomeSettings` reestructurado: `biomeTintedProps` vs `defaultProps` + `foliageColor` + `terrainLayer`
7. ✅ `PlaceTintedProp()` con `MaterialPropertyBlock` (_TopColor) sin romper GPU Instancing
8. ✅ `PlaceDecorativeProp()` mantiene materiales originales para defaultProps
9. ✅ `BiomeSample.BlendColor()` para interpolar colores entre biomas
10. ✅ Herramienta de Splatmap en `WorldGeneratorPro` (EditorWindow): pinta alphamaps según `WorldBiomeMap`
11. ✅ Bonus: perturbación de Perlin Noise para praderas (parches irregulares)

### 🛠️ Pendiente: setup en Unity
1. Crear `BiomeSettings` assets (Create → World → Biome Settings):
   - `BosqueFragoso.asset` — treeDensity: 0.85, foliageColor: verde oscuro
   - `Pradera.asset` — treeDensity: 0.05, foliageColor: verde claro/amarillo
   - `Ciudad.asset` — usesManualLayoutOnly: true
2. Crear TerrainLayers para cada bioma y asignarlos en `BiomeSettings.terrainLayer`:
   - Se pueden usar `Ground_Layer_01` y `Ground_Layer_02` de Polytope como base
3. Asignar prefabs de Polytope en las listas correctas:
   - **biomeTintedTrees/Understory/GroundCover**: pasto, arbustos base → heredan foliageColor
   - **treeTypes/rockTypes/etc**: árboles con textura propia, rocas → mantienen color original
4. Verificar que los materiales de los biomeTintedProps tengan shader Polytope
   con `_CUSTOMCOLORSTINTING` (se activa automáticamente al instanciar)
5. Generar terreno → esculpir manualmente → usar botón "🎨 Aplicar Splatmap" en WorldGeneratorPro
   - Esfera gris escalada (1.5x1x1.5) → "RocaPlaceholder" → asignar como `rockTypes[0]`
3. Agregar `WorldBiomeMap` a la escena (GameObject vacío, asignar `defaultBiome` y primer control point)
4. Verificar que `WorldBiomeMap` esté en la escena **antes** de que `WorldChunkManager` cargue
5. Cuando haya modelos reales: reemplazar los prefabs en los assets, sin tocar código