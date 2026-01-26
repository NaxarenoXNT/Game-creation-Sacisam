# Object Pooling

## Visión General

Object Pooling es un patrón de optimización que reutiliza objetos en lugar de crear/destruir constantemente. Esto evita la fragmentación de memoria y reduce el trabajo del Garbage Collector.

```
Sin Pooling:
    Instantiate() → Usar → Destroy() → Instantiate() → Usar → Destroy()
    [GC frecuente, lag spikes]

Con Pooling:
    Pool → Obtener → Usar → Devolver → Obtener → Usar → Devolver
    [Sin GC adicional, rendimiento estable]
```

---

## ObjectPool<T>

**Archivo**: `Assets/Scripts/Managers/ObjectPool.cs`

### Definición

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class ObjectPool<T> where T : Component
    {
        private T prefab;
        private Transform contenedor;
        private Queue<T> objetosDisponibles = new Queue<T>();
        private List<T> objetosActivos = new List<T>();
        
        public ObjectPool(T prefab, int cantidadInicial, Transform contenedor = null)
        {
            this.prefab = prefab;
            this.contenedor = contenedor;
            
            // Pre-crear objetos
            for (int i = 0; i < cantidadInicial; i++)
            {
                CrearNuevo();
            }
        }
        
        private T CrearNuevo()
        {
            T obj = Object.Instantiate(prefab, contenedor);
            obj.gameObject.SetActive(false);
            objetosDisponibles.Enqueue(obj);
            return obj;
        }
        
        public T Obtener()
        {
            T obj;
            
            if (objetosDisponibles.Count > 0)
            {
                obj = objetosDisponibles.Dequeue();
            }
            else
            {
                obj = CrearNuevo();
                objetosDisponibles.Dequeue();  // Sacarlo de la cola
            }
            
            obj.gameObject.SetActive(true);
            objetosActivos.Add(obj);
            return obj;
        }
        
        public void Devolver(T obj)
        {
            obj.gameObject.SetActive(false);
            objetosActivos.Remove(obj);
            objetosDisponibles.Enqueue(obj);
        }
        
        public void DevolverTodos()
        {
            foreach (var obj in objetosActivos.ToArray())
            {
                Devolver(obj);
            }
        }
    }
}
```

---

## Uso Básico

### Crear Pool

```csharp
public class GestorProyectiles : MonoBehaviour
{
    [SerializeField] private Proyectil prefabProyectil;
    [SerializeField] private int cantidadInicial = 20;
    [SerializeField] private Transform contenedor;
    
    private ObjectPool<Proyectil> poolProyectiles;
    
    void Awake()
    {
        poolProyectiles = new ObjectPool<Proyectil>(
            prefabProyectil, 
            cantidadInicial, 
            contenedor
        );
    }
}
```

### Obtener Objeto

```csharp
public void Disparar(Vector3 posicion, Vector3 direccion)
{
    // Obtener proyectil del pool
    Proyectil proyectil = poolProyectiles.Obtener();
    
    // Configurar
    proyectil.transform.position = posicion;
    proyectil.Inicializar(direccion, 10f);  // velocidad
}
```

### Devolver Objeto

```csharp
// En el script del proyectil
public class Proyectil : MonoBehaviour
{
    private System.Action<Proyectil> onDevolver;
    
    public void ConfigurarDevolucion(System.Action<Proyectil> callback)
    {
        onDevolver = callback;
    }
    
    public void Desactivar()
    {
        // Devolver al pool
        onDevolver?.Invoke(this);
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Hacer daño, efectos, etc.
        Desactivar();
    }
}
```

---

## Casos de Uso

### 1. Números de Daño Flotantes

```csharp
public class GestorNumerosDano : MonoBehaviour
{
    [SerializeField] private NumeroDano prefab;
    [SerializeField] private int poolSize = 30;
    
    private ObjectPool<NumeroDano> pool;
    
    void Start()
    {
        pool = new ObjectPool<NumeroDano>(prefab, poolSize, transform);
        
        // Suscribirse a eventos de daño
        EventBus.Suscribir<EventoDano>(OnDano);
    }
    
    private void OnDano(EventoDano e)
    {
        NumeroDano numero = pool.Obtener();
        numero.Mostrar(e.Cantidad, e.EsCritico, () => pool.Devolver(numero));
    }
}
```

```csharp
public class NumeroDano : MonoBehaviour
{
    [SerializeField] private Text texto;
    
    private System.Action onTerminar;
    
    public void Mostrar(int cantidad, bool critico, System.Action callback)
    {
        onTerminar = callback;
        texto.text = cantidad.ToString();
        texto.color = critico ? Color.yellow : Color.red;
        
        StartCoroutine(Animar());
    }
    
    private IEnumerator Animar()
    {
        float duracion = 1f;
        float tiempo = 0;
        Vector3 inicio = transform.position;
        
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            transform.position = inicio + Vector3.up * tiempo;
            yield return null;
        }
        
        onTerminar?.Invoke();
    }
}
```

### 2. Efectos de Partículas

```csharp
public class GestorEfectos : MonoBehaviour
{
    [SerializeField] private ParticleSystem prefabExplosion;
    [SerializeField] private ParticleSystem prefabCuracion;
    
    private ObjectPool<ParticleSystem> poolExplosiones;
    private ObjectPool<ParticleSystem> poolCuraciones;
    
    void Start()
    {
        poolExplosiones = new ObjectPool<ParticleSystem>(prefabExplosion, 10, transform);
        poolCuraciones = new ObjectPool<ParticleSystem>(prefabCuracion, 5, transform);
    }
    
    public void ReproducirExplosion(Vector3 posicion)
    {
        var efecto = poolExplosiones.Obtener();
        efecto.transform.position = posicion;
        efecto.Play();
        
        // Devolver después de la duración
        StartCoroutine(DevolverDespues(efecto, poolExplosiones, efecto.main.duration));
    }
    
    private IEnumerator DevolverDespues<T>(T obj, ObjectPool<T> pool, float delay) where T : Component
    {
        yield return new WaitForSeconds(delay);
        pool.Devolver(obj);
    }
}
```

### 3. Enemigos Spawneados

```csharp
public class SpawnerEnemigos : MonoBehaviour
{
    [SerializeField] private Goblin prefabGoblin;
    [SerializeField] private int poolSize = 15;
    
    private ObjectPool<Goblin> poolGoblins;
    
    void Start()
    {
        poolGoblins = new ObjectPool<Goblin>(prefabGoblin, poolSize, transform);
    }
    
    public Goblin SpawnearGoblin(Vector3 posicion)
    {
        Goblin goblin = poolGoblins.Obtener();
        goblin.transform.position = posicion;
        goblin.Reiniciar();  // Resetear stats, estados, etc.
        
        // Cuando muera, devolver al pool
        goblin.OnMuerte += () => poolGoblins.Devolver(goblin);
        
        return goblin;
    }
}
```

---

## Componente Reiniciable

Para objetos del pool, implementar interfaz de reinicio:

```csharp
public interface IReiniciable
{
    void Reiniciar();
}

public class Goblin : Enemigos, IReiniciable
{
    public void Reiniciar()
    {
        // Restaurar vida
        Stats.VidaActual = Stats.VidaMaxima;
        
        // Limpiar estados
        GestorEstados.LimpiarTodos();
        
        // Reiniciar IA
        InicializarIA();
        
        // Limpiar eventos
        OnMuerte = null;
    }
}
```

---

## Pool Genérico con Callback

Versión mejorada con callback de inicialización:

```csharp
public class ObjectPool<T> where T : Component
{
    private System.Func<T> crearObjeto;
    private System.Action<T> onObtener;
    private System.Action<T> onDevolver;
    
    // ... constructor con callbacks
    
    public T Obtener()
    {
        T obj;
        
        if (objetosDisponibles.Count > 0)
            obj = objetosDisponibles.Dequeue();
        else
            obj = crearObjeto();
        
        obj.gameObject.SetActive(true);
        onObtener?.Invoke(obj);
        objetosActivos.Add(obj);
        
        return obj;
    }
    
    public void Devolver(T obj)
    {
        onDevolver?.Invoke(obj);
        obj.gameObject.SetActive(false);
        objetosActivos.Remove(obj);
        objetosDisponibles.Enqueue(obj);
    }
}
```

### Uso con Callbacks

```csharp
var pool = new ObjectPool<Proyectil>(
    crearObjeto: () => Instantiate(prefab, contenedor),
    onObtener: (p) => p.Activar(),
    onDevolver: (p) => p.Desactivar()
);
```

---

## Diagrama de Flujo

```
       ┌─────────────────────────────────────┐
       │         Object Pool                 │
       │                                     │
       │  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐   │
       │  │ Obj │ │ Obj │ │ Obj │ │ Obj │   │  ← Objetos inactivos
       │  └─────┘ └─────┘ └─────┘ └─────┘   │
       └─────────────────────────────────────┘
                        │
                        │ Obtener()
                        ▼
              ┌─────────────────┐
              │   Activar Obj   │
              │   SetActive(true)│
              └─────────────────┘
                        │
                        │ Usar en el juego
                        ▼
              ┌─────────────────┐
              │   Objeto Activo │
              │   en escena     │
              └─────────────────┘
                        │
                        │ Devolver()
                        ▼
              ┌─────────────────┐
              │  Desactivar Obj │
              │  SetActive(false)│
              └─────────────────┘
                        │
                        │ De vuelta al pool
                        ▼
       ┌─────────────────────────────────────┐
       │         Object Pool                 │
       │  (objeto disponible de nuevo)       │
       └─────────────────────────────────────┘
```

---

## Buenas Prácticas

### ✅ Hacer

```csharp
// Pre-calentar el pool con objetos suficientes
poolProyectiles = new ObjectPool<Proyectil>(prefab, 50, contenedor);

// Siempre reiniciar el estado del objeto
public void Reiniciar()
{
    Stats.VidaActual = Stats.VidaMaxima;
    transform.position = Vector3.zero;
}

// Usar contenedor para organizar en jerarquía
var contenedor = new GameObject("Pool_Proyectiles").transform;
```

### ❌ Evitar

```csharp
// NO destruir objetos del pool
Destroy(objetoDelPool);  // MAL

// NO olvidar devolver objetos
poolProyectiles.Obtener();  // Usar
// Olvidar devolver = memory leak

// NO mezclar objetos de diferentes pools
poolA.Devolver(objetoDePoolB);  // MAL
```

---

## Cuándo Usar Pooling

| Situación | ¿Usar Pool? |
|-----------|-------------|
| Proyectiles frecuentes | ✅ Sí |
| Números de daño | ✅ Sí |
| Efectos de partículas | ✅ Sí |
| Enemigos en oleadas | ✅ Sí |
| Bosses únicos | ❌ No |
| UI estática | ❌ No |
| Objetos que aparecen 1 vez | ❌ No |

---

## Rendimiento

### Sin Pooling
```
Frame 1: Instantiate() → 2ms
Frame 2: Destroy() → GC 5ms ← Spike
Frame 3: Instantiate() → 2ms
...
```

### Con Pooling
```
Frame 0: Crear pool → 20ms (una sola vez)
Frame 1: Obtener() → 0.01ms
Frame 2: Devolver() → 0.01ms
Frame 3: Obtener() → 0.01ms
...
```

El costo inicial se amortiza con el uso continuo.
