# Guía Rápida de Setup - Sistema de Pooling Dinámico

> **📖 Ver también:**  
> - Documentación técnica: [14_ObjectPool_Refactor.md](14_ObjectPool_Refactor.md)  
> - Integración con chunks: [GUIA_CHUNK_INTEGRATION.md](GUIA_CHUNK_INTEGRATION.md)  
> - Sistema completo: [25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md)

---

## Paso 1: Crear el Manager en la Escena

1. En la jerarquía, crear un GameObject vacío
2. Nombrarlo `DynamicEnemyPoolManager`
3. Agregar el componente `DynamicEnemyPoolManager`
4. En el Inspector, asignar:
   - **Enemy Controller Prefab**: Tu prefab de EnemyController
   - **Tiempo Limpieza Pools**: 120 (segundos)
   - **Tamaño Inicial Pool**: 5
   - **Tamaño Máximo Pool**: 20

> ⚠️ **Importante:** Este GameObject se marca con `DontDestroyOnLoad` automáticamente.

---

## Paso 2: Asegurarte de que EnemyController está en un Prefab

Tu prefab de EnemyController debe tener:
- ✅ Componente `EnemyController`
- ✅ Componente `EntityStats`
- ✅ Componente `Renderer` (para efectos visuales)
- ✅ Componente `Animator` (opcional)
- ❌ **NO** asignar `EnemigoData` en el inspector del prefab
  - Se asignará dinámicamente al obtener del pool

---

## Paso 3: Crear tus EnemigoData

Si no tienes EnemigoData creados:

1. Click derecho en Project
2. Create → ScriptableObjects → EnemigoData
3. Configurar stats, elementos, etc.
4. Repetir para cada tipo de enemigo

---

## Paso 4: Usar el Sistema

### Opción A: Desde código (Recomendado)

```csharp
using Managers;

public class MiSpawner : MonoBehaviour
{
    [SerializeField] private EnemigoData goblinData;
    
    void SpawnearGoblin(Vector3 posicion)
    {
        // Obtener controller del pool
        var controller = DynamicEnemyPoolManager.Instance.ObtenerController(goblinData);
        
        if (controller != null)
        {
            controller.transform.position = posicion;
            // El controller ya está inicializado con goblinData
        }
    }
}
```

### Opción B: Usar el ejemplo incluido

1. Crear un GameObject vacío llamado "EnemySpawner"
2. Agregar componente `EnemySpawnerExample`
3. En el Inspector:
   - **Tipos De Enemigos**: Agregar tus EnemigoData
   - **Spawn Points**: Agregar transforms donde spawear
   - **Spawn Interval**: 3 segundos

---

## Paso 5: Suscribirse a Eventos (Opcional)

Para reaccionar cuando un enemigo muere:

```csharp
using Managers;

public class MiSistema : MonoBehaviour
{
    void Start()
    {
        EventBus.Suscribir<EventoEnemigoDerrotado>(OnEnemigoDerrotado);
    }
    
    void OnDestroy()
    {
        EventBus.Desuscribir<EventoEnemigoDerrotado>(OnEnemigoDerrotado);
    }
    
    void OnEnemigoDerrotado(EventoEnemigoDerrotado evento)
    {
        Debug.Log($"Enemigo derrotado: {evento.NombreEnemigo}");
        Debug.Log($"XP ganada: {evento.XPOtorgada}");
        Debug.Log($"Posición: {evento.PosicionMuerte}");
        
        // Aquí puedes:
        // - Dar XP al jugador
        // - Drop de items
        // - Actualizar misiones
        // - Actualizar estadísticas
    }
}
```

---

## Debugging y Monitoring

### Ver estado de los pools en runtime

1. Seleccionar el GameObject `DynamicEnemyPoolManager`
2. Click derecho en el componente
3. **Debug: Mostrar Estado** → Muestra info en consola

### Comandos útiles:
- **Debug: Mostrar Estado**: Estadísticas de todos los pools
- **Debug: Devolver Todos**: Devuelve todos los enemigos a sus pools
- **Debug: Limpiar Pools No Usados**: Libera memoria de pools vacíos

### Verificar performance

```csharp
// En tu código de debug
if (Input.GetKeyDown(KeyCode.P))
{
    var manager = DynamicEnemyPoolManager.Instance;
    Debug.Log($"Pools activos: {manager.TotalActivePools}");
    Debug.Log($"Enemigos activos: {manager.TotalActiveEnemies}");
}
```

---

## Troubleshooting

### "EnemyController prefab no está asignado"
→ Asigna el prefab en el Inspector del DynamicEnemyPoolManager

### "EnemigoData es null"
→ Verifica que estás pasando un ScriptableObject válido a `ObtenerController()`

### "No se puede obtener de ObjectPool destruido"
→ El pool fue destruido prematuramente, verifica que DynamicEnemyPoolManager existe

### Los enemigos no se devuelven al pool
→ Verifica que `ManejarMuerte()` en EnemyController se está ejecutando

### Memory leaks / GC spikes
→ Verifica que `OnDevueltoAlPool()` está limpiando todos los eventos
→ Usa Unity Profiler para identificar allocations

---

## Mejores Prácticas

### ✅ DO:
- Pre-cargar pools al inicio de zonas grandes
- Verificar que DynamicEnemyPoolManager.Instance existe antes de usar
- Usar EventoEnemigoDerrotado en lugar de referencias directas
- Limpiar eventos en OnDestroy()

### ❌ DON'T:
- NO destruir manualmente GameObjects de enemigos (usa el pool)
- NO guardar referencias a EnemyController entre frames (puede reciclarse)
- NO poolear personajes del jugador (solo enemigos genéricos)
- NO crear múltiples DynamicEnemyPoolManager (es Singleton)

---

## Optimizaciones Avanzadas

### Pre-carga inteligente por zona

```csharp
public class ZoneLoader : MonoBehaviour
{
    [SerializeField] private ZoneEnemyConfig config;
    
    void OnZoneEnter()
    {
        // Pre-cargar todos los tipos de esta zona
        foreach (var enemyType in config.enemyTypes)
        {
            int expectedCount = enemyType.maxSimultaneous;
            DynamicEnemyPoolManager.Instance.PrecargarPool(
                enemyType.data, 
                expectedCount, 
                expectedCount * 2
            );
        }
    }
    
    void OnZoneExit()
    {
        // Opcionalmente limpiar pools de esta zona
        // (el manager lo hará automáticamente después de 240s)
    }
}
```

---

## Next Steps

Ahora que el sistema de pooling está funcionando, puedes:

1. **Crear PersistentEnemyManager** (opcional)
   - Para que enemigos muertos no reaparezcan
   - Guardado de estado por instancia

2. **Agregar Behaviors** (opcional)
   - PatrolBehavior para patrullas
   - GuardBehavior para guardias
   - TrapAwareBehavior para IA inteligente

3. **Integrar con Quest System** (futuro)
   - Suscribirse a EventoEnemigoDerrotado
   - Actualizar objetivos de misiones

4. **Integrar con Save System** (futuro)
   - Guardar estado de enemigos persistentes
   - Respawn configurable

---

¿Dudas? Revisa:
- `Documentation/23_IntegrationsSystem.md` - Documentación completa
- `Documentation/CORRECCIONES_APLICADAS.md` - Resumen de cambios
- `Assets/Scripts/Examples/EnemySpawnerExample.cs` - Código de ejemplo
