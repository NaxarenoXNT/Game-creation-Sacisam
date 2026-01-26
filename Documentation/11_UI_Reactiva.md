# Sistema de UI Reactiva

## Visión General

El sistema de UI reactiva usa el patrón Observer para que la interfaz se actualice automáticamente cuando cambian los datos, sin polling ni actualizaciones manuales.

```
Observable<T>
    │
    ├── Valor actual
    └── Lista de suscriptores
            │
            ├── BarraVida.OnVidaCambiada()
            ├── TextoNivel.OnNivelCambiado()
            └── PanelEstados.OnEstadoCambiado()
```

---

## Observable<T>

**Archivo**: `Assets/Scripts/UI/UIReactiva.cs`

### Definición

```csharp
public class Observable<T>
{
    private T valor;
    private event Action<T> onChange;
    
    public T Valor
    {
        get => valor;
        set
        {
            if (!Equals(valor, value))
            {
                valor = value;
                onChange?.Invoke(valor);
            }
        }
    }
    
    public void Suscribir(Action<T> callback)
    {
        onChange += callback;
        callback?.Invoke(valor);  // Notifica valor inicial
    }
    
    public void Desuscribir(Action<T> callback)
    {
        onChange -= callback;
    }
}
```

### Uso Básico

```csharp
// Crear observable
var vida = new Observable<int>();
vida.Valor = 100;

// Suscribirse a cambios
vida.Suscribir(nuevaVida => {
    Debug.Log("Vida cambió a: " + nuevaVida);
});

// Cuando cambia el valor, se notifica automáticamente
vida.Valor = 75;  // → "Vida cambió a: 75"
```

---

## Componentes de UI

### BarraReactiva

Barra de progreso que se actualiza automáticamente.

```csharp
using UnityEngine;
using UnityEngine.UI;

public class BarraReactiva : MonoBehaviour
{
    [SerializeField] private Image imagenBarra;
    [SerializeField] private Text textoValor;  // Opcional
    [SerializeField] private float velocidadAnimacion = 5f;
    
    private float valorObjetivo;
    private float valorActual;
    
    public void Vincular(Observable<float> observable)
    {
        observable.Suscribir(OnValorCambiado);
    }
    
    public void SetValor(float actual, float maximo)
    {
        valorObjetivo = actual / maximo;
        
        if (textoValor != null)
            textoValor.text = $"{actual:0}/{maximo:0}";
    }
    
    private void OnValorCambiado(float porcentaje)
    {
        valorObjetivo = porcentaje;
    }
    
    void Update()
    {
        // Animar suavemente
        valorActual = Mathf.Lerp(valorActual, valorObjetivo, 
                                 Time.deltaTime * velocidadAnimacion);
        imagenBarra.fillAmount = valorActual;
    }
}
```

### Configurar en Unity

1. Crear UI → Image (llamarlo "BarraFondo")
2. Hijo: Image (llamarlo "BarraRelleno")
3. BarraRelleno: Image Type = Filled, Fill Method = Horizontal
4. Agregar componente `BarraReactiva` al padre
5. Asignar BarraRelleno a `imagenBarra`

---

## PanelEntidad

Panel completo para mostrar stats de una entidad.

```csharp
using UnityEngine;
using UnityEngine.UI;

public class PanelEntidad : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Text textoNombre;
    [SerializeField] private Text textoNivel;
    [SerializeField] private BarraReactiva barraVida;
    [SerializeField] private BarraReactiva barraMana;  // Opcional
    [SerializeField] private Transform contenedorEstados;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject prefabIconoEstado;
    
    private Entidad entidadVinculada;
    
    public void Vincular(Entidad entidad)
    {
        entidadVinculada = entidad;
        
        // Configurar nombre
        textoNombre.text = entidad.Nombre;
        
        // Suscribirse a eventos
        EventBus.Suscribir<EventoVidaCambiada>(OnVidaCambiada);
        EventBus.Suscribir<EventoNivelSubido>(OnNivelSubido);
        EventBus.Suscribir<EventoEstadoAplicado>(OnEstadoAplicado);
        EventBus.Suscribir<EventoEstadoRemovido>(OnEstadoRemovido);
        
        // Actualizar valores iniciales
        ActualizarUI();
    }
    
    void OnDestroy()
    {
        EventBus.Desuscribir<EventoVidaCambiada>(OnVidaCambiada);
        EventBus.Desuscribir<EventoNivelSubido>(OnNivelSubido);
        EventBus.Desuscribir<EventoEstadoAplicado>(OnEstadoAplicado);
        EventBus.Desuscribir<EventoEstadoRemovido>(OnEstadoRemovido);
    }
    
    private void OnVidaCambiada(EventoVidaCambiada e)
    {
        if (e.Entidad != entidadVinculada) return;
        barraVida.SetValor(e.VidaActual, e.VidaMaxima);
    }
    
    private void OnNivelSubido(EventoNivelSubido e)
    {
        if (e.Jugador != entidadVinculada) return;
        textoNivel.text = "Nv. " + e.NivelNuevo;
    }
    
    private void ActualizarUI()
    {
        var stats = entidadVinculada.Stats;
        barraVida.SetValor(stats.VidaActual, stats.VidaMaxima);
        
        if (entidadVinculada is Jugador jugador)
            textoNivel.text = "Nv. " + jugador.Nivel;
    }
}
```

---

## BotonHabilidad

Botón interactivo para habilidades con cooldown visual.

```csharp
using UnityEngine;
using UnityEngine.UI;

public class BotonHabilidad : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Button boton;
    [SerializeField] private Image icono;
    [SerializeField] private Image fondoCooldown;
    [SerializeField] private Text textoCooldown;
    [SerializeField] private Text textoNombre;
    
    private HabilidadData habilidad;
    private IGestorHabilidades gestorCooldowns;
    private System.Action<HabilidadData> onHabilidadSeleccionada;
    
    public void Configurar(HabilidadData habilidad, IGestorHabilidades gestor, 
                          System.Action<HabilidadData> callback)
    {
        this.habilidad = habilidad;
        this.gestorCooldowns = gestor;
        this.onHabilidadSeleccionada = callback;
        
        // Configurar visuales
        textoNombre.text = habilidad.nombre;
        if (habilidad.icono != null)
            icono.sprite = habilidad.icono;
        
        boton.onClick.AddListener(OnClick);
    }
    
    void Update()
    {
        if (habilidad == null || gestorCooldowns == null) return;
        
        int turnosRestantes = gestorCooldowns.ObtenerCooldownRestante(habilidad);
        bool enCooldown = turnosRestantes > 0;
        
        // Actualizar visuales
        boton.interactable = !enCooldown;
        fondoCooldown.gameObject.SetActive(enCooldown);
        textoCooldown.gameObject.SetActive(enCooldown);
        
        if (enCooldown)
        {
            textoCooldown.text = turnosRestantes.ToString();
            fondoCooldown.fillAmount = (float)turnosRestantes / habilidad.cooldown;
        }
    }
    
    private void OnClick()
    {
        onHabilidadSeleccionada?.Invoke(habilidad);
    }
}
```

---

## Integración con Entidad

### Crear Observables en EntityStats

```csharp
public class EntityStats
{
    // Observables para UI reactiva
    public Observable<float> VidaPorcentaje { get; } = new Observable<float>();
    
    private int vidaActual;
    public int VidaActual
    {
        get => vidaActual;
        set
        {
            vidaActual = Mathf.Clamp(value, 0, VidaMaxima);
            VidaPorcentaje.Valor = (float)vidaActual / VidaMaxima;
        }
    }
    
    // ... resto de propiedades
}
```

### Vincular en el Controlador

```csharp
public class EntityController : MonoBehaviour
{
    [SerializeField] private PanelEntidad panelUI;
    
    void Start()
    {
        if (panelUI != null)
        {
            panelUI.Vincular(entidad);
        }
    }
}
```

---

## Ejemplo Completo: HUD del Jugador

### Estructura en Unity

```
Canvas (Screen Space - Overlay)
    └── PanelJugador (PanelEntidad.cs)
            ├── TextoNombre
            ├── TextoNivel
            ├── BarraVida (BarraReactiva.cs)
            │       ├── Fondo
            │       └── Relleno
            ├── ContenedorEstados
            │       └── (iconos dinámicos)
            └── ContenedorHabilidades
                    ├── BotonHabilidad1
                    ├── BotonHabilidad2
                    └── BotonHabilidad3
```

### Script de Control

```csharp
public class HUDJugador : MonoBehaviour
{
    [SerializeField] private PanelEntidad panelEntidad;
    [SerializeField] private Transform contenedorHabilidades;
    [SerializeField] private GameObject prefabBotonHabilidad;
    
    private Jugador jugador;
    
    public void Inicializar(Jugador jugador)
    {
        this.jugador = jugador;
        
        // Vincular panel de entidad
        panelEntidad.Vincular(jugador);
        
        // Crear botones de habilidades
        CrearBotonesHabilidades();
    }
    
    private void CrearBotonesHabilidades()
    {
        // Limpiar botones existentes
        foreach (Transform child in contenedorHabilidades)
            Destroy(child.gameObject);
        
        // Crear botón por cada habilidad
        foreach (var habilidad in jugador.Habilidades)
        {
            var botonGO = Instantiate(prefabBotonHabilidad, contenedorHabilidades);
            var boton = botonGO.GetComponent<BotonHabilidad>();
            
            boton.Configurar(habilidad, jugador.GestorCooldowns, OnHabilidadSeleccionada);
        }
    }
    
    private void OnHabilidadSeleccionada(HabilidadData habilidad)
    {
        // Notificar al sistema de combate
        EventBus.Publicar(new EventoHabilidadSeleccionada
        {
            Jugador = jugador,
            Habilidad = habilidad
        });
    }
}
```

---

## Animaciones

### Flash de Daño

```csharp
public class FlashDano : MonoBehaviour
{
    [SerializeField] private Image imagen;
    [SerializeField] private Color colorFlash = Color.red;
    [SerializeField] private float duracion = 0.2f;
    
    private Color colorOriginal;
    
    void Start()
    {
        colorOriginal = imagen.color;
        EventBus.Suscribir<EventoDano>(OnDano);
    }
    
    void OnDestroy()
    {
        EventBus.Desuscribir<EventoDano>(OnDano);
    }
    
    private void OnDano(EventoDano e)
    {
        // Verificar si es nuestra entidad
        // ...
        StartCoroutine(AnimarFlash());
    }
    
    private IEnumerator AnimarFlash()
    {
        imagen.color = colorFlash;
        yield return new WaitForSeconds(duracion);
        imagen.color = colorOriginal;
    }
}
```

### Número Flotante

```csharp
public class NumeroFlotante : MonoBehaviour
{
    [SerializeField] private Text texto;
    [SerializeField] private float duracion = 1f;
    [SerializeField] private float alturaMaxima = 50f;
    
    public void Mostrar(string valor, Color color)
    {
        texto.text = valor;
        texto.color = color;
        StartCoroutine(Animar());
    }
    
    private IEnumerator Animar()
    {
        Vector3 posInicial = transform.position;
        float tiempo = 0;
        
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracion;
            
            // Mover hacia arriba
            transform.position = posInicial + Vector3.up * (t * alturaMaxima);
            
            // Desvanecer
            var color = texto.color;
            color.a = 1 - t;
            texto.color = color;
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
}
```

---

## Diagrama de Flujo

```
Jugador recibe daño
        │
        ▼
Entidad.RecibirDano()
        │
        ├─► Stats.VidaActual = X
        │           │
        │           ▼
        │   Observable notifica
        │           │
        │           ▼
        │   BarraReactiva.OnValorCambiado()
        │           │
        │           ▼
        │   Barra se actualiza visualmente
        │
        └─► EventBus.Publicar(EventoDano)
                    │
                    ├──────────┐
                    ▼          ▼
            FlashDano    NumeroFlotante
                │              │
                ▼              ▼
        Pantalla roja    "-25" aparece
```

---

## Buenas Prácticas

### ✅ Hacer

```csharp
// Siempre desuscribirse
void OnDestroy()
{
    observable.Desuscribir(MiCallback);
}

// Usar eventos para sincronizar UI
EventBus.Suscribir<EventoVidaCambiada>(ActualizarBarra);

// Animar cambios para feedback visual
```

### ❌ Evitar

```csharp
// NO hacer polling en Update
void Update()
{
    // MAL: verificar cada frame
    barraVida.fillAmount = jugador.Stats.VidaActual / jugador.Stats.VidaMaxima;
}

// NO olvidar desuscribirse (memory leaks)

// NO modificar datos desde UI (solo mostrar)
```
