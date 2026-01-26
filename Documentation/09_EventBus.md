# Sistema de Eventos (EventBus)

## Visión General

EventBus permite comunicación desacoplada entre sistemas. En lugar de que un sistema llame directamente a otro, publica eventos que cualquier interesado puede escuchar.

```
Publicador                          Suscriptores
    │                                    │
    │    EventBus.Publicar(evento)       │
    ├───────────────────────────────────►│ UI
    │                                    │ Audio
    │                                    │ Logros
    │                                    │ Analytics
```

---

## EventBus (Singleton)

**Archivo**: `Assets/Scripts/Managers/EventBus.cs`

### Métodos Estáticos

| Método | Descripción |
|--------|-------------|
| `Suscribir<T>(callback)` | Registrar un listener para eventos tipo T |
| `Desuscribir<T>(callback)` | Remover un listener |
| `Publicar<T>(evento)` | Enviar un evento a todos los suscriptores |
| `LimpiarTodo()` | Remover todos los suscriptores |

---

## Tipos de Eventos

### Eventos de Combate

```csharp
// Cuando se inflige daño
public class EventoDano
{
    public Entidad Origen { get; set; }
    public Entidad Objetivo { get; set; }
    public int Cantidad { get; set; }
    public ElementFlag Elemento { get; set; }
    public bool EsCritico { get; set; }
}

// Cuando una entidad muere
public class EventoMuerte
{
    public Entidad Entidad { get; set; }
    public Entidad Asesino { get; set; }
}

// Cuando se usa una habilidad
public class EventoHabilidadUsada
{
    public Entidad Lanzador { get; set; }
    public HabilidadData Habilidad { get; set; }
    public Entidad[] Objetivos { get; set; }
}
```

### Eventos de Entidad

```csharp
// Cuando cambia la vida
public class EventoVidaCambiada
{
    public Entidad Entidad { get; set; }
    public int VidaAnterior { get; set; }
    public int VidaActual { get; set; }
    public int VidaMaxima { get; set; }
}

// Cuando sube de nivel
public class EventoNivelSubido
{
    public Jugador Jugador { get; set; }
    public int NivelAnterior { get; set; }
    public int NivelNuevo { get; set; }
}

// Cuando se aplica un estado
public class EventoEstadoAplicado
{
    public Entidad Entidad { get; set; }
    public StatusFlag Estado { get; set; }
    public int Duracion { get; set; }
}
```

### Eventos de Progresión

```csharp
// Cuando se gana experiencia
public class EventoExperienciaGanada
{
    public Jugador Jugador { get; set; }
    public int Cantidad { get; set; }
    public int ExpTotal { get; set; }
}

// Cuando se obtiene un item
public class EventoItemObtenido
{
    public string ItemId { get; set; }
    public int Cantidad { get; set; }
}
```

---

## Uso Básico

### Suscribirse a Eventos

```csharp
public class UIVida : MonoBehaviour
{
    void OnEnable()
    {
        EventBus.Suscribir<EventoVidaCambiada>(OnVidaCambiada);
        EventBus.Suscribir<EventoMuerte>(OnMuerte);
    }
    
    void OnDisable()
    {
        EventBus.Desuscribir<EventoVidaCambiada>(OnVidaCambiada);
        EventBus.Desuscribir<EventoMuerte>(OnMuerte);
    }
    
    private void OnVidaCambiada(EventoVidaCambiada evento)
    {
        // Actualizar barra de vida
        float porcentaje = (float)evento.VidaActual / evento.VidaMaxima;
        barraVida.fillAmount = porcentaje;
        textoVida.text = $"{evento.VidaActual}/{evento.VidaMaxima}";
    }
    
    private void OnMuerte(EventoMuerte evento)
    {
        // Mostrar animación de muerte
        panelMuerte.SetActive(true);
    }
}
```

### Publicar Eventos

```csharp
// En Entidad.cs cuando recibe daño
public void RecibirDano(int cantidad, Entidad origen = null, bool esCritico = false)
{
    int vidaAnterior = Stats.VidaActual;
    Stats.VidaActual = Mathf.Max(0, Stats.VidaActual - cantidad);
    
    // Publicar evento de daño
    EventBus.Publicar(new EventoDano
    {
        Origen = origen,
        Objetivo = this,
        Cantidad = cantidad,
        EsCritico = esCritico
    });
    
    // Publicar evento de vida cambiada
    EventBus.Publicar(new EventoVidaCambiada
    {
        Entidad = this,
        VidaAnterior = vidaAnterior,
        VidaActual = Stats.VidaActual,
        VidaMaxima = Stats.VidaMaxima
    });
    
    // Si murió, publicar evento de muerte
    if (Stats.VidaActual <= 0)
    {
        EventBus.Publicar(new EventoMuerte
        {
            Entidad = this,
            Asesino = origen
        });
    }
}
```

---

## Ejemplos por Sistema

### Sistema de Audio

```csharp
public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioClip sonidoDano;
    [SerializeField] private AudioClip sonidoCritico;
    [SerializeField] private AudioClip sonidoMuerte;
    [SerializeField] private AudioClip sonidoNivelSubido;
    
    void OnEnable()
    {
        EventBus.Suscribir<EventoDano>(OnDano);
        EventBus.Suscribir<EventoMuerte>(OnMuerte);
        EventBus.Suscribir<EventoNivelSubido>(OnNivelSubido);
    }
    
    void OnDisable()
    {
        EventBus.Desuscribir<EventoDano>(OnDano);
        EventBus.Desuscribir<EventoMuerte>(OnMuerte);
        EventBus.Desuscribir<EventoNivelSubido>(OnNivelSubido);
    }
    
    private void OnDano(EventoDano e)
    {
        if (e.EsCritico)
            AudioSource.PlayClipAtPoint(sonidoCritico, Vector3.zero);
        else
            AudioSource.PlayClipAtPoint(sonidoDano, Vector3.zero);
    }
    
    private void OnMuerte(EventoMuerte e)
    {
        AudioSource.PlayClipAtPoint(sonidoMuerte, Vector3.zero);
    }
    
    private void OnNivelSubido(EventoNivelSubido e)
    {
        AudioSource.PlayClipAtPoint(sonidoNivelSubido, Vector3.zero);
    }
}
```

### Sistema de Logros

```csharp
public class LogrosManager : MonoBehaviour
{
    private int enemigosDerotados = 0;
    
    void OnEnable()
    {
        EventBus.Suscribir<EventoMuerte>(OnMuerte);
        EventBus.Suscribir<EventoNivelSubido>(OnNivelSubido);
    }
    
    void OnDisable()
    {
        EventBus.Desuscribir<EventoMuerte>(OnMuerte);
        EventBus.Desuscribir<EventoNivelSubido>(OnNivelSubido);
    }
    
    private void OnMuerte(EventoMuerte e)
    {
        // Si el que murió es un enemigo
        if (e.Entidad is Enemigos)
        {
            enemigosDerotados++;
            
            if (enemigosDerotados == 10)
                DesbloquearLogro("Cazador Novato");
            else if (enemigosDerotados == 100)
                DesbloquearLogro("Cazador Experto");
        }
    }
    
    private void OnNivelSubido(EventoNivelSubido e)
    {
        if (e.NivelNuevo >= 10)
            DesbloquearLogro("Nivel 10");
    }
}
```

### Sistema de Números Flotantes

```csharp
public class DamageNumberSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prefabNumero;
    
    void OnEnable()
    {
        EventBus.Suscribir<EventoDano>(OnDano);
        EventBus.Suscribir<EventoVidaCambiada>(OnCuracion);
    }
    
    void OnDisable()
    {
        EventBus.Desuscribir<EventoDano>(OnDano);
        EventBus.Desuscribir<EventoVidaCambiada>(OnCuracion);
    }
    
    private void OnDano(EventoDano e)
    {
        // Mostrar número de daño rojo
        var numero = Instantiate(prefabNumero);
        numero.GetComponent<DamageNumber>().Mostrar(
            e.Cantidad.ToString(),
            e.EsCritico ? Color.yellow : Color.red,
            ObtenerPosicion(e.Objetivo)
        );
    }
    
    private void OnCuracion(EventoVidaCambiada e)
    {
        // Si la vida aumentó, mostrar curación verde
        if (e.VidaActual > e.VidaAnterior)
        {
            int curacion = e.VidaActual - e.VidaAnterior;
            var numero = Instantiate(prefabNumero);
            numero.GetComponent<DamageNumber>().Mostrar(
                "+" + curacion,
                Color.green,
                ObtenerPosicion(e.Entidad)
            );
        }
    }
}
```

---

## Buenas Prácticas

### ✅ Hacer

```csharp
// Siempre desuscribirse al desactivarse
void OnDisable()
{
    EventBus.Desuscribir<EventoDano>(OnDano);
}

// Usar eventos para comunicación entre sistemas
EventBus.Publicar(new EventoVidaCambiada { ... });

// Crear eventos específicos con datos relevantes
public class EventoMuerte
{
    public Entidad Entidad { get; set; }
    public Entidad Asesino { get; set; }  // Útil para dar crédito
}
```

### ❌ Evitar

```csharp
// NO olvidar desuscribirse (causa memory leaks)
void OnEnable()
{
    EventBus.Suscribir<EventoDano>(OnDano);
    // Si no te desuscribes, el objeto nunca se libera
}

// NO usar eventos para lógica crítica de gameplay
// Los eventos son "fire and forget", no garantizan orden

// NO modificar el estado del juego en múltiples handlers
// Puede causar condiciones de carrera
```

---

## Diagrama de Flujo

```
Jugador usa Ataque Pesado
        │
        ▼
DamageEffect.Aplicar()
        │
        ▼
Enemigo.RecibirDano(50)
        │
        ├─────────────────────────────────┐
        ▼                                 ▼
EventBus.Publicar(EventoDano)    EventBus.Publicar(EventoVidaCambiada)
        │                                 │
        ├──────────┐                      ├──────────┐
        ▼          ▼                      ▼          ▼
   AudioManager  UIPopups            BarraVida   LogrosManager
        │          │                      │          │
        ▼          ▼                      ▼          ▼
   PlaySound   ShowNumber            UpdateBar  CheckLogro
```

---

## Limpiar al Cambiar Escena

```csharp
// En un GameManager o similar
public class GameManager : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Limpiar eventos al cargar nueva escena
        EventBus.LimpiarTodo();
    }
}
```
