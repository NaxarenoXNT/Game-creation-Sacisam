# 14. Sistema ObjectPool - Arquitectura Refactorizada

> Documentación del sistema de Object Pooling modular.
> Para integración con el sistema de enemigos dinámicos ver [25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md).

## Índice

- [Archivos Asociados](#archivos-asociados)
- [Visión General](#visión-general)
- [Arquitectura Modular](#arquitectura-modular)
- [Componentes del Sistema](#componentes-del-sistema)
- [API Pública — Singleton](#api-pública--singleton)
- [API Pública — Pool Genérico](#api-pública--pool-genérico)
- [Uso y Ejemplos](#uso-y-ejemplos)
- [Optimizaciones de Performance](#optimizaciones-de-performance)
- [Mejores Prácticas](#mejores-prácticas)

---

## Archivos Asociados

| Archivo | Descripción |
|---------|-------------|
| [Assets/Scripts/Managers/ObjectPool/IPooleable.cs](../Assets/Scripts/Managers/ObjectPool/IPooleable.cs) | Interfaz de callbacks para objetos pooleados |
| [Assets/Scripts/Managers/ObjectPool/PoolStats.cs](../Assets/Scripts/Managers/ObjectPool/PoolStats.cs) | Struct de estadísticas de un pool |
| [Assets/Scripts/Managers/ObjectPool/PoolConfig.cs](../Assets/Scripts/Managers/ObjectPool/PoolConfig.cs) | Clase de configuración serializable |
| [Assets/Scripts/Managers/ObjectPool/PoolLogic.cs](../Assets/Scripts/Managers/ObjectPool/PoolLogic.cs) | Lógica core de un pool individual (`internal`) |
| [Assets/Scripts/Managers/ObjectPool/ObjectPoolGeneric.cs](../Assets/Scripts/Managers/ObjectPool/ObjectPoolGeneric.cs) | Pool genérico tipado `ObjectPool<T>` |
| [Assets/Scripts/Managers/ObjectPool/PooledObject.cs](../Assets/Scripts/Managers/ObjectPool/PooledObject.cs) | Componente auxiliar de tracking en objetos instanciados |
| [Assets/Scripts/Managers/ObjectPool/ObjectPool.cs](../Assets/Scripts/Managers/ObjectPool/ObjectPool.cs) | Singleton `MonoBehaviour` — fachada principal |

---

## Visión General

El sistema ObjectPool está estructurado de forma modular. Todos los archivos pertenecen al namespace `Managers`.

### Objetivos del Diseño

- **Eliminar código duplicado** mediante la clase interna `PoolLogic`
- **Separar responsabilidades** — cada archivo tiene un único propósito
- **Sin locks** — optimizado para Unity (main thread only)
- **Compatibilidad total** con la API pública existente

---

## Arquitectura Modular

### Estructura de Archivos

```
Assets/Scripts/Managers/ObjectPool/
├── IPooleable.cs            (21 líneas)  — Interfaz de callbacks
├── PoolStats.cs             (31 líneas)  — Struct de estadísticas
├── PoolConfig.cs            (25 líneas)  — Configuración serializable
├── PoolLogic.cs            (282 líneas)  — Lógica core (internal)
├── ObjectPoolGeneric.cs    (278 líneas)  — Pool genérico ObjectPool<T>
├── PooledObject.cs          (39 líneas)  — Componente auxiliar
└── ObjectPool.cs           (333 líneas)  — Singleton (fachada)
```

### Diagrama de Dependencias

```
┌─────────────────────────────────────────────────┐
│            ObjectPool (Singleton)               │
│              MonoBehaviour                      │
└────────────────┬────────────────────────────────┘
                 │ Dictionary<string, PoolLogic>
                 ▼
┌─────────────────────────────────────────────────┐
│              PoolLogic (internal)               │
│        Lógica core de un pool individual        │
└────────┬─────────────────────────────────┬──────┘
         │                                 │
         ▼                                 ▼
┌────────────────┐              ┌──────────────────┐
│  PoolConfig    │              │  PooledObject    │
│ Configuración  │              │   (tracking)     │
└────────────────┘              └──────────────────┘
                                          │
                                          ▼
┌─────────────────────────────────────────────────┐
│              IPooleable (interface)             │
│         OnObtenidoDelPool / OnDevueltoAlPool    │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│          ObjectPool<T> (Genérico)               │
│  Uso autónomo — no depende del Singleton        │
│  También usa IPooleable y PoolStats             │
└─────────────────────────────────────────────────┘
```

---

## Componentes del Sistema

### IPooleable.cs

```csharp
namespace Managers
{
    public interface IPooleable
    {
        void OnObtenidoDelPool();   // Llamado al salir del pool (activación)
        void OnDevueltoAlPool();    // Llamado al volver al pool (desactivación)
    }
}
```

Implementa esta interfaz para reiniciar/limpiar estado en cualquier componente pooleado.

---

### PoolStats.cs

```csharp
namespace Managers
{
    public struct PoolStats
    {
        public string PoolId;
        public int TotalCreados;
        public int TotalReusos;
        public int Activos;
        public int Disponibles;
        public int Total;
        public int TamanoMaximo;
        public float RatioReuso;
        public bool Destruido;

        public bool EsEficiente => RatioReuso >= 0.8f;
        public override string ToString(); // Formato legible para Debug.Log
    }
}
```

---

### PoolConfig.cs

```csharp
namespace Managers
{
    [System.Serializable]
    public class PoolConfig
    {
        public string poolId;
        public GameObject prefab;
        public int tamanoInicial = 10;
        public int tamanoMaximo = 50;
        public bool expandirSiNecesario = true;
        public bool autoReturn = false;
        public float autoReturnDelay = 2f;
        public bool reutilizarMasAntiguo = true;
    }
}
```

Serializable — configurable desde el Inspector o por código.

---

### PoolLogic.cs (internal)

Clase `internal` que encapsula la lógica de **un único pool**. Solo `ObjectPool` Singleton puede instanciarla.

**Propiedades públicas:**
```csharp
int TotalCreados
int TotalReusos
int ActiveCount
int AvailableCount
int TotalCount
bool IsDestroyed
```

**Métodos públicos:**
```csharp
GameObject Obtener()
GameObject Obtener(Vector3 posicion, Quaternion rotacion)
void Devolver(GameObject obj)
void DevolverTodos()
PoolStats GetStats()
void Destruir()
```

**Comportamiento interno:**
- `Queue<GameObject>` para disponibles, `List<GameObject>` para activos, `HashSet<GameObject>` para validación O(1)
- Pre-crea objetos en el constructor hasta `tamanoInicial`
- Detecta y limpia objetos destruidos externamente
- Llama callbacks `IPooleable` en todos los componentes del objeto
- Coroutine de auto-return gestionada por el `MonoBehaviour` del Singleton
- Al devolver: resetea position/rotation/scale locales y reparenta al contenedor

---

### ObjectPoolGeneric.cs

Pool independiente del Singleton, tipado por componente. Implementa `IDisposable`.

```csharp
namespace Managers
{
    public class ObjectPool<T> : IDisposable where T : Component
    {
        // Propiedades
        int TotalCreated      // Nota: nombre en inglés (vs TotalCreados en PoolLogic)
        int TotalReusos
        int ActiveCount
        int AvailableCount
        int TotalCount

        // Constructor
        ObjectPool(T prefab, int cantidadInicial, Transform contenedor = null,
                   int maxSize = -1, bool allowGrowth = true,
                   bool autoReturn = false, float autoReturnDelay = 2f)

        // Métodos
        T Obtener()
        T Obtener(Vector3 posicion, Quaternion rotacion)
        void Devolver(T obj)
        void DevolverTodos()
        PoolStats GetStats()
        void Destruir()
        void Dispose()    // llama Destruir()
    }
}
```

**Diferencias con el Singleton:**
- Tipado — devuelve `T` directamente, sin casteo
- Autónomo — no depende de `ObjectPool.Instance`
- El auto-return usa una coroutine en el propio componente `T` (debe ser `MonoBehaviour`)
- `GetStats()` usa `typeof(T).Name` como `PoolId`

---

### PooledObject.cs

Componente que se agrega automáticamente a cada objeto instanciado por `PoolLogic`.

```csharp
namespace Managers
{
    public class PooledObject : MonoBehaviour, IPooleable
    {
        public string PoolId { get; set; }

        public void DevolverAlPool();
        public void DevolverAlPoolDespuesDe(float delay);

        public virtual void OnObtenidoDelPool();  // override para reiniciar
        public virtual void OnDevueltoAlPool();   // llama StopAllCoroutines()
    }
}
```

Permite subclasear: extender `PooledObject` en lugar de implementar `IPooleable` desde cero.

---

### ObjectPool.cs (Singleton)

`MonoBehaviour` con `DontDestroyOnLoad`. Gestiona un `Dictionary<string, PoolLogic>`.

```csharp
namespace Managers
{
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; }

        // Crear pools
        void CrearPool(PoolConfig config)
        void CrearPool(string poolId, GameObject prefab, int tamanoInicial = 10,
                       int tamanoMaximo = 50, bool autoReturn = false,
                       float autoReturnDelay = 2f, bool reutilizarMasAntiguo = true)

        // Obtener objetos
        GameObject Obtener(string poolId)
        GameObject Obtener(string poolId, Vector3 posicion, Quaternion rotacion)

        // Devolver objetos
        void Devolver(GameObject obj)               // usa PooledObject para determinar el poolId
        void Devolver(string poolId, GameObject obj)
        void DevolverDespuesDe(GameObject obj, float delay)
        void DevolverTodos(string poolId)

        // Consultas
        int ObtenerDisponibles(string poolId)
        int ObtenerActivos(string poolId)
        int ObtenerTotal(string poolId)
        PoolStats ObtenerEstadisticas(string poolId)

        // Limpieza
        void LimpiarPool(string poolId)
        void LimpiarTodo()
    }
}
```

**Inicialización:** Los pools configurados en el Inspector (lista `configuraciones`) se crean en `Awake()`.

**Métodos de debug (`[ContextMenu]`):**
- `"Debug: Mostrar Estado"` — imprime `PoolStats.ToString()` de todos los pools activos
- `"Debug: Devolver Todos"` — devuelve todos los objetos activos a sus pools

---

## API Pública — Singleton

### Crear Pools

```csharp
// Opción 1: Con PoolConfig
var config = new PoolConfig {
    poolId = "Projectile",
    prefab = projectilePrefab,
    tamanoInicial = 20,
    tamanoMaximo = 100,
    autoReturn = true,
    autoReturnDelay = 3f
};
ObjectPool.Instance.CrearPool(config);

// Opción 2: Inline desde código
ObjectPool.Instance.CrearPool(
    poolId: "VFX_Explosion",
    prefab: explosionPrefab,
    tamanoInicial: 10,
    tamanoMaximo: 30,
    autoReturn: true,
    autoReturnDelay: 2f
);
```

### Obtener Objetos

```csharp
// Simple
GameObject projectile = ObjectPool.Instance.Obtener("Projectile");

// Con posición y rotación
GameObject vfx = ObjectPool.Instance.Obtener("VFX_Hit", hitPosition, Quaternion.identity);
```

### Devolver Objetos

```csharp
// Inmediato (el componente PooledObject determina el pool)
ObjectPool.Instance.Devolver(projectile);

// Con delay
ObjectPool.Instance.DevolverDespuesDe(vfx, 2f);

// Todos los activos de un pool
ObjectPool.Instance.DevolverTodos("Projectile");
```

### Estadísticas y Debugging

```csharp
PoolStats stats = ObjectPool.Instance.ObtenerEstadisticas("Projectile");
Debug.Log(stats.ToString());
// => "=== Pool<Projectile> Stats ===\n  Creados: X | Reusos: Y ..."

Debug.Log($"Activos: {stats.Activos} / {stats.Total}");
Debug.Log($"Eficiente: {stats.EsEficiente}");  // true si RatioReuso >= 80%

int disponibles = ObjectPool.Instance.ObtenerDisponibles("Projectile");
int activos     = ObjectPool.Instance.ObtenerActivos("Projectile");
int total       = ObjectPool.Instance.ObtenerTotal("Projectile");
```

---

## API Pública — Pool Genérico

```csharp
public class DynamicEnemyPoolManager : MonoBehaviour
{
    [SerializeField] private EnemyController enemyPrefab;
    private ObjectPool<EnemyController> enemyPool;

    private void Awake()
    {
        enemyPool = new ObjectPool<EnemyController>(
            prefab: enemyPrefab,
            cantidadInicial: 10,
            contenedor: transform,
            maxSize: 50,
            allowGrowth: true
        );
    }

    public EnemyController SpawnEnemy(Vector3 position)
        => enemyPool.Obtener(position, Quaternion.identity);

    public void ReturnEnemy(EnemyController enemy)
        => enemyPool.Devolver(enemy);

    private void OnDestroy() => enemyPool?.Destruir();
}
```

---

## Uso y Ejemplos

### Ejemplo 1: Sistema de Proyectiles

```csharp
public class ProjectileManager : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;

    private void Start()
    {
        ObjectPool.Instance.CrearPool("Bullet", bulletPrefab, tamanoInicial: 30, tamanoMaximo: 100);
    }

    public void FireBullet(Vector3 origin, Vector3 direction)
    {
        GameObject bullet = ObjectPool.Instance.Obtener("Bullet", origin, Quaternion.identity);
        bullet.GetComponent<Bullet>().Initialize(direction);
    }
}

public class Bullet : MonoBehaviour, IPooleable
{
    private Vector3 velocity;

    public void Initialize(Vector3 direction) => velocity = direction * 10f;

    private void Update() => transform.position += velocity * Time.deltaTime;

    public void OnObtenidoDelPool()  => velocity = Vector3.zero;
    public void OnDevueltoAlPool()   => StopAllCoroutines();

    private void OnCollisionEnter(Collision _) => ObjectPool.Instance.Devolver(gameObject);
}
```

---

### Ejemplo 2: VFX con Auto-Return

```csharp
ObjectPool.Instance.CrearPool(
    poolId: "VFX_Hit",
    prefab: hitEffectPrefab,
    tamanoInicial: 15,
    tamanoMaximo: 50,
    autoReturn: true,       // se devuelve solo
    autoReturnDelay: 2f
);

// No necesitas devolver manualmente
ObjectPool.Instance.Obtener("VFX_Hit", position, rotation);
```

---

### Ejemplo 3: Subclase de PooledObject

```csharp
// En lugar de implementar IPooleable desde cero, extender PooledObject
public class EnemyPooled : PooledObject
{
    private int health;

    public override void OnObtenidoDelPool()
    {
        base.OnObtenidoDelPool();
        health = 100;
        GetComponent<Collider>().enabled = true;
    }

    public override void OnDevueltoAlPool()
    {
        base.OnDevueltoAlPool();   // llama StopAllCoroutines()
        GetComponent<Collider>().enabled = false;
    }
}
```

---

### Ejemplo 4: Pool Genérico Tipado

```csharp
public class ParticlePoolManager : MonoBehaviour
{
    [SerializeField] private ParticleSystem particlePrefab;
    private ObjectPool<ParticleSystem> particlePool;

    private void Awake()
    {
        particlePool = new ObjectPool<ParticleSystem>(
            prefab: particlePrefab,
            cantidadInicial: 20,
            contenedor: transform,
            maxSize: 100,
            autoReturn: true,
            autoReturnDelay: 5f
        );
    }

    public void PlayParticle(Vector3 position)
    {
        ParticleSystem ps = particlePool.Obtener(position, Quaternion.identity);
        ps.Play();
    }

    [ContextMenu("Show Pool Stats")]
    private void ShowStats() => Debug.Log(particlePool.GetStats());
}
```

---

## Optimizaciones de Performance

### Sin Locks

Unity corre en main thread. No hay locks en ningún método.
Resultado estimado: ~15-20% mejora en llamadas `Obtener()` y `Devolver()`.

### Estructuras de Datos

| Estructura | Uso | Beneficio |
|------------|-----|-----------|
| `Queue<T>` | Disponibles | FIFO, O(1) enqueue/dequeue |
| `List<T>` | Activos | Acceso por índice para reutilizar el más antiguo |
| `HashSet<T>` | Todos los objetos | Validación de pertenencia O(1) |

### Reutilización del Objeto Más Antiguo

Cuando el pool está lleno y `reutilizarMasAntiguo = true`, el sistema reutiliza el primer elemento activo en lugar de fallar:

```csharp
// PoolLogic.ReutilizarMasAntiguo()
GameObject oldest = objetosActivos[0];
objetosActivos.RemoveAt(0);
NotificarDevuelto(oldest);
NotificarObtenido(oldest);
objetosActivos.Add(oldest);
return oldest;
```

---

## Mejores Prácticas

### Recomendaciones

1. **Implementa `IPooleable`** o extiende `PooledObject` para reiniciar/limpiar estado correctamente.

2. **Configura tamaños apropiados** — monitorea con `PoolStats`:
   ```csharp
   var stats = ObjectPool.Instance.ObtenerEstadisticas("Enemies");
   if (stats.Activos >= stats.TamanoMaximo * 0.9f)
       Debug.LogWarning("Pool cerca del límite");
   ```

3. **Usa `autoReturn`** para efectos temporales (VFX, partículas, audio).

4. **Usa `ObjectPool<T>`** para sistemas con tipo específico — evita casteos y es type-safe.

5. **Limpia pools en transiciones de escena:**
   ```csharp
   ObjectPool.Instance.LimpiarPool("Enemies");
   ```

### Anti-patrones

```csharp
// ❌ NUNCA destruir un objeto pooleado
Destroy(pooledObject);

// ✅ Siempre devolver al pool
ObjectPool.Instance.Devolver(pooledObject);
```

```csharp
// ❌ Pool demasiado pequeño para objetos frecuentes
ObjectPool.Instance.CrearPool("Bullet", bulletPrefab, 5, 10);

// ✅ Tamaño apropiado al uso real
ObjectPool.Instance.CrearPool("Bullet", bulletPrefab, 50, 200);
```

```csharp
// ❌ Olvidar limpiar referencias en OnDevueltoAlPool
public void OnDevueltoAlPool() { }

// ✅ Limpiar para evitar memory leaks
public void OnDevueltoAlPool()
{
    target = null;
    owner = null;
    StopAllCoroutines();
}
```

---

**Última actualización:** Marzo 2026