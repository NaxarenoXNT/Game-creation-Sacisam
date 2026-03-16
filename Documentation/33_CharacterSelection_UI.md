# 33 — Sistema de Selección de Personaje (UI Toolkit)

Pantalla de creación de party inicial antes de cargar la escena de gameplay.  
Tecnología: **Unity UI Toolkit** (UXML + USS + C#).

---

## Archivos del sistema

| Archivo | Ubicación | Tipo | Responsabilidad |
|---------|-----------|------|-----------------|
| `CharacterSelectionConfig.cs` | Scripts/CharacterSelection/ | ScriptableObject | Configuración: clases, prefab, límites, escena destino |
| `CharacterSelectionManager.cs` | Scripts/CharacterSelection/ | MonoBehaviour | Lógica: crear/eliminar personajes, instanciar, cargar escena |
| `CharacterSelectionUI.cs` | Scripts/CharacterSelection/ | MonoBehaviour | Controlador UI Toolkit: conecta UXML con el Manager |
| `CharacterSelectionBootstrap.cs` | Scripts/CharacterSelection/ | MonoBehaviour | Inicializa singletons antes de que la UI arranque |
| `CharacterSelection.uxml` | UI_Toolkit/ | UXML | Layout de tres paneles |
| `CharacterSelection.uss` | UI_Toolkit/ | USS | Estilos (paleta oscura semitransparente) |
| `CharacterSelectionConfig.asset` | Resources/ | Asset | Instancia configurada del SO |

---

## Arquitectura: flujo de datos

```
CharacterSelectionBootstrap.Awake()
    └─ Garantiza PlayerPartyManager.Instance existe
            ↓
CharacterSelectionUI.OnEnable()
    ├─ CacheElements()        ← Q<T>("name") sobre el UXML
    ├─ SetupCallbacks()       ← BtnCrear.clicked, BtnIniciar.clicked
    ├─ PopulateClassList()    ← genera botones desde Config.clasesDisponibles
    ├─ RefreshPartyList()
    └─ RefreshButtons()
            ↓
  [Usuario hace click en una clase]
    └─ SelectClass(clase, btn)
        ├─ Marca class-button--selected en el botón
        └─ UpdatePreview(clase)
            ├─ ClassName.text, ClassDescription.text
            ├─ ClassIcon.style.backgroundImage
            ├─ SetStatBar() ×5  ← Length.Percent normalizado
            ├─ AbilitiesList → Labels con .ability-chip
            └─ PasivasList   → Labels con .passive-chip
            ↓
  [Usuario escribe nombre y hace click en "Crear Personaje"]
    └─ CharacterSelectionManager.CrearPersonaje(clase, nombre)
        ├─ Valida límites (PuedeCrearMas)
        ├─ new CharacterCreationData { characterId = Guid, ... }
        ├─ esMain = (primero creado)
        └─ OnPersonajeCreado?.Invoke(data)
            → CharacterSelectionUI.OnPersonajeCreado()
                └─ RefreshPartyList() + RefreshButtons()
            ↓
  [Usuario hace click en "¡Comenzar Aventura!"]
    └─ CharacterSelectionManager.IniciarJuego()
        ├─ Instancia config.playerPrefab × cada personaje (DontDestroyOnLoad)
        ├─ controller.Inicializar(clase)
        ├─ PlayerPartyManager.RegisterCharacter + AddToActiveParty
        ├─ PlayerPartyManager.SetMainCharacter (el de esMain)
        ├─ new EvolutionState per personaje
        ├─ MissionManager.RegistrarPersonaje (si existe)
        └─ SceneManager.LoadScene(config.escenaDestino)
```

---

## Layout UXML — tres paneles

```
selection-root
├── Title                    "SELECCIÓN DE PERSONAJE"
├── main-content (row)
│   ├── classes-panel (izq, 160px)
│   │   ├── "CLASES DISPONIBLES"  (.section-title)
│   │   └── ClassList (ScrollView)
│   │       └── [Button × clase]  .class-button / .class-button--selected
│   │           ├── VisualElement  .class-icon-small
│   │           └── Label          .class-btn-name
│   │
│   ├── preview-panel (centro, flex-grow)
│   │   ├── row: ClassIcon (.class-icon) + ClassName + ClassDescription
│   │   ├── stats-grid
│   │   │   └── stat-row ×5: .stat-label | .stat-bar-bg > BarXxx | ValXxx
│   │   ├── "HABILIDADES INICIALES" + AbilitiesList (.ability-chip)
│   │   ├── "PASIVAS INICIALES"    + PasivasList   (.passive-chip)
│   │   ├── name-input-row: CharacterName (TextField)
│   │   └── BtnCrear
│   │
│   └── party-panel (der, 155px)
│       ├── "TU PARTY"  (.section-title)
│       ├── PartyCount  "N / 4 personajes"
│       ├── PartyList (ScrollView)
│       │   └── [VisualElement × personaje creado]  .party-card
│       │       ├── VisualElement  .party-avatar
│       │       ├── VisualElement  .party-slot-info
│       │       │   ├── Label  .party-char-name
│       │       │   ├── Label  .party-char-class
│       │       │   └── Label  .party-slot-main-badge ("★ MAIN", si aplica)
│       │       └── Button  .btn-remove ("✕")
│       └── BtnIniciar
└── Footer
```

> Los `name` del UXML **no cambian**. Si necesitás referenciar un elemento desde
> C# usá siempre `_root.Q<T>("Name")`.

---

## Barras de stats — normalización

Las barras se setean por porcentaje desde `SetStatBar()`:

```csharp
private void SetStatBar(VisualElement bar, Label valLabel, float value, float max)
{
    float pct = Mathf.Clamp01(value / max) * 100f;
    bar.style.width = new Length(pct, LengthUnit.Percent);
    valLabel.text = Mathf.RoundToInt(value).ToString();
}
```

Valores máximos actuales (ajustar si una clase supera el techo):

```csharp
private const float MAX_VIDA      = 2000f;
private const float MAX_ATAQUE    = 150f;
private const float MAX_DEFENSA   = 100f;
private const float MAX_MANA      = 200f;
private const float MAX_VELOCIDAD = 150f;
```

Stats reales de las clases actualmente configuradas:

| Clase | Vida | Ataque | Defensa | Mana | Velocidad |
|-------|------|--------|---------|------|-----------|
| Arquero | 800 | 90 | 20 | 50 | 80 |
| Mago | 1000 | 100 | 20 | 50 | 50 |
| Guerrero | 1500 | 80 | 50 | 50 | 100 |

---

## CharacterSelectionConfig (ScriptableObject)

```
Assets/Resources/CharacterSelectionConfig.asset
```

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `clasesDisponibles` | `List<ClaseData>` | — | Clases que aparecen en el panel izquierdo |
| `playerPrefab` | `GameObject` | — | Prefab con `EntityController` + `EntityStats` |
| `maxPersonajesInicial` | `int` | 1 | Máximo personajes creables |
| `minPersonajesRequeridos` | `int` | 1 | Mínimo para habilitar "Comenzar" |
| `escenaDestino` | `string` | `"Mundo"` | Nombre de la escena de gameplay |

---

## CharacterSelectionManager — API pública

| Miembro | Tipo | Descripción |
|---------|------|-------------|
| `PersonajesCreados` | `IReadOnlyList<CharacterCreationData>` | Lista de personajes creados |
| `Config` | `CharacterSelectionConfig` | SO activo |
| `PuedeCrearMas` | `bool` | `Count < maxPersonajesInicial` |
| `PuedeIniciar` | `bool` | `Count >= minPersonajesRequeridos` |
| `OnPersonajeCreado` | `event Action<CharacterCreationData>` | Disparado al crear |
| `OnPersonajeEliminado` | `event Action<int>` | Disparado al eliminar (índice) |
| `OnInicioJuego` | `event Action` | Disparado justo antes de cargar escena |
| `CrearPersonaje(clase, nombre)` | `bool` | Crea y agrega al listado |
| `EliminarPersonaje(index)` | `bool` | Elimina y reasigna main si era el primero |
| `EstablecerMain(index)` | `void` | Cambia el personaje principal |
| `IniciarJuego()` | `void` | Instancia, registra en managers y carga escena |

---

## CharacterCreationData

```csharp
public class CharacterCreationData
{
    public string   characterId;  // GUID generado al crear
    public string   nombre;
    public ClaseData clase;
    public bool     esMain;       // true solo en uno
}
```

---

## USS — clases CSS principales

| Clase | Dónde se usa |
|-------|-------------|
| `.selection-root` | Contenedor raíz |
| `.title-label` | Título principal |
| `.classes-panel` | Panel izquierdo |
| `.class-button` | Botón de clase (generado en C#) |
| `.class-button--selected` | Estado seleccionado del botón de clase |
| `.class-icon-small` | Ícono 26×26 dentro del botón |
| `.preview-panel` | Panel central |
| `.class-icon` | Ícono grande 52×52 de la clase |
| `.stat-bar-vida/ataque/defensa/mana/velocidad` | Colores de cada barra |
| `.ability-chip` | Chip de habilidad activa (fondo violeta) |
| `.passive-chip` | Chip de pasiva (fondo verde) |
| `.party-panel` | Panel derecho |
| `.party-card` | Tarjeta de personaje creado |
| `.party-avatar` | Ícono 28×28 del personaje en party |
| `.party-char-name` / `.party-char-class` | Nombre y clase en la tarjeta |
| `.party-slot-main` | Borde resaltado en el personaje main |
| `.party-slot-main-badge` | Badge "★ MAIN" |
| `.btn-remove` | Botón ✕ para eliminar de la party |
| `.btn-crear` | Botón "Crear Personaje" |
| `.btn-iniciar` | Botón "¡Comenzar Aventura!" (verde, :disabled → opacity 0.4) |
| `.footer-label` | Texto de ayuda inferior |

---

## Setup de escena

1. Crear escena `CharacterSelection`.
2. Agregar **UIDocument** → asignar `CharacterSelection.uxml` como SourceAsset, `PanelSettings.asset` como PanelSettings.
3. Crear GameObject `SelectionManager`:
   - Añadir `CharacterSelectionManager` → asignar `CharacterSelectionConfig.asset`.
   - Añadir `CharacterSelectionUI` → asignar el mismo Manager.
4. Crear GameObject `Bootstrap`:
   - Añadir `CharacterSelectionBootstrap`.
5. En `CharacterSelectionConfig.asset`:
   - Agregar las `ClaseData` deseadas a `clasesDisponibles`.
   - Asignar `playerPrefab`.
   - Configurar `escenaDestino = "Mundo"` (o la escena que corresponda).

---

## Integración con otros sistemas

| Sistema | Punto de integración |
|---------|---------------------|
| `PlayerPartyManager` | `IniciarJuego()` llama `RegisterCharacter` + `AddToActiveParty` + `SetMainCharacter` |
| `EntityController` | Se instancia desde `config.playerPrefab` y se inicializa con `Inicializar(clase)` |
| `EvolutionState` | Se crea uno por personaje con `characterId` y `nivelJugador = 1` |
| `MissionManager` | `RegistrarPersonaje(characterId, evolutionState)` si existe en escena |
| `SceneManager` | `LoadScene(config.escenaDestino)` al terminar |

Ver también: [18_Sistema_Party](18_Sistema_Party.md) · [03_Clases_Jugador](03_Clases_Jugador.md) · [19_Evoluciones](19_Evoluciones.md)
