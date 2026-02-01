# Sistema de Party y Personajes

## Visión General

El sistema de party permite al jugador tener **múltiples personajes** con uno como **main** (controlado activamente). Los demás pueden estar en el **party activo** (siguen al main), **estacionados** (hibernando en ubicaciones) o disponibles como **refuerzos**.

```
┌─────────────────────────────────────────────────────────────────┐
│                    GESTIÓN DE PERSONAJES                        │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              PlayerPartyManager (Singleton)              │   │
│  │                                                          │   │
│  │   ┌───────────┐  ┌────────────┐  ┌─────────────────┐   │   │
│  │   │   Main    │  │Active Party│  │   Stationed     │   │   │
│  │   │(Controlado)│ │ (Siguen)   │  │  (Hibernando)   │   │   │
│  │   └─────┬─────┘  └──────┬─────┘  └────────┬────────┘   │   │
│  │         │               │                  │            │   │
│  │         └───────────────┼──────────────────┘            │   │
│  │                         ▼                               │   │
│  │              allOwnedCharacters (Max: 20)               │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              ReinforcementSystem                         │   │
│  │  (Gestiona llegada de refuerzos durante combate)        │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## PlayerPartyManager

**Archivo**: `Assets/Scripts/Managers/PlayerPartyManager.cs`

Singleton que gestiona todos los personajes del jugador.

### Configuración

```csharp
[Header("Configuración")]
[SerializeField] private int maxOwnedCharacters = 20;      // Máximo total
[SerializeField] private int maxActivePartySize = 5;       // Máximo en party activo
[SerializeField] private float maxReinforcementDistance = 100f;
[SerializeField] private float distancePerTurn = 20f;      // Velocidad de refuerzos
```

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `MainCharacter` | EntityController | Personaje actualmente controlado |
| `MainTransform` | Transform | Transform del main (para detección) |
| `ActiveParty` | IReadOnlyList | Party que sigue al main |
| `AllOwnedCharacters` | IReadOnlyList | Todos los personajes |
| `CanAddMoreCharacters` | bool | Si hay espacio para más |
| `CanAddToActiveParty` | bool | Si hay espacio en party activo |

### Estados de un Personaje

```
┌─────────────┐     SetMainCharacter()     ┌─────────────┐
│    Main     │◄──────────────────────────►│Active Party │
│ (Solo 1)   │                             │  (Max 5)    │
└─────────────┘                             └──────┬──────┘
      ▲                                           │
      │         StationCharacter()                │
      │        ────────────────────►              ▼
      │                               ┌─────────────────┐
      │                               │   Stationed     │
      │                               │ (Hibernando)    │
      └───────────────────────────────└─────────────────┘
              RecallCharacter()
```

### Eventos

```csharp
// Disparado cuando cambia el main
event Action<EntityController, EntityController> OnMainChanged; // (old, new)

// Disparado cuando alguien entra/sale del party
event Action<EntityController> OnCharacterJoinedParty;
event Action<EntityController> OnCharacterLeftParty;

// Disparado al registrar nuevo personaje
event Action<EntityController> OnCharacterRegistered;

// Disparado al estacionar
event Action<EntityController, Vector3> OnCharacterStationed;
```

---

## Gestión de Personajes

### Registrar Personaje

```csharp
// Registrar nuevo personaje (creación, reclutamiento, etc.)
bool success = PlayerPartyManager.Instance.RegisterCharacter(nuevoPersonaje);

// El primer personaje registrado automáticamente es el main
// Marca al personaje con IsPlayerOwned = true
```

### Cambiar Main

```csharp
// Cambiar quién controla el jugador
PlayerPartyManager.Instance.SetMainCharacter(otroPersonaje);

// El main anterior:
// - Si keepOldMainInParty = true → Va al party activo
// - Si keepOldMainInParty = false → Se estaciona
```

### Party Activo

```csharp
// Agregar al party (siguen al main)
PlayerPartyManager.Instance.AddToActiveParty(personaje);

// Remover del party
PlayerPartyManager.Instance.RemoveFromActiveParty(personaje, stationAtCurrentLocation: true);

// Verificar party
var party = PlayerPartyManager.Instance.ActiveParty;
int count = PlayerPartyManager.Instance.ActivePartyCount;
```

### Estacionar Personaje

```csharp
// Estacionar en ubicación con nombre
PlayerPartyManager.Instance.StationCharacter(personaje, "Aldea Norte");

// Recuperar personaje estacionado
PlayerPartyManager.Instance.RecallCharacter(personaje);

// Obtener info de estacionamiento
var info = PlayerPartyManager.Instance.GetStationedInfo(personaje);
// info.Position, info.LocationName, info.StationedTime
```

---

## Sistema de Refuerzos

**Archivo**: `Assets/Scripts/Managers/ReinforcementSystem.cs`

Permite llamar aliados durante un combate. Llegan después de varios turnos según la distancia.

### Configuración

```csharp
[Header("Configuración")]
[SerializeField] private float distancePerTurn = 20f;  // Unidades por turno
[SerializeField] private int maxPendingReinforcements = 3;
[SerializeField] private bool autoProcessOnTurnStart = true;
```

### Cálculo de Turnos de Llegada

```csharp
turnosParaLlegar = Ceil(distancia / distancePerTurn)

// Ejemplo:
// Distancia = 45 unidades
// distancePerTurn = 20
// turnos = Ceil(45/20) = 3 turnos
```

### Solicitar Refuerzos

```csharp
// Obtener refuerzos disponibles
var disponibles = PlayerPartyManager.Instance.GetAvailableReinforcements(combatPosition);

// Cada ReinforcementInfo contiene:
// - Character: EntityController
// - Distance: float
// - TurnsToArrive: int

// Solicitar todos los disponibles
ReinforcementSystem.Instance.RequestReinforcements(combatPosition);

// Solicitar uno específico
ReinforcementSystem.Instance.RequestSpecificReinforcement(personaje, combatPosition);
```

### Flujo de Refuerzos

```
1. SOLICITUD
   └── RequestReinforcements(posición)
       └── Calcula distancia y turnos
       └── Programa llegadas

2. CADA TURNO
   └── OnTurnStart → ProcessArrivingReinforcements()
       └── ¿Llegó algún refuerzo?
           └── Sí → AddReinforcementToCombat()

3. LLEGADA
   └── CombateManager.AgregarAliadoAlCombate()
   └── PlayerPartyManager.AddToActiveParty()
   └── Publicar EventoRefuerzoLlegado

4. FIN COMBATE
   └── Cancelar refuerzos pendientes
   └── Publicar EventoRefuerzosCancelados
```

---

## Eventos del Sistema de Party

### EventBus Events

```csharp
// Personaje registrado
EventoPersonajeRegistrado { Personaje }

// Main cambió
EventoMainCambiado { MainAnterior, NuevoMain }

// Party
EventoPersonajeUnidoParty { Personaje, TamanoPartyActual }
EventoPersonajeSalioParty { Personaje, FueEstacionado }

// Estacionamiento
EventoPersonajeEstacionado { Personaje, Ubicacion, NombreUbicacion }

// Refuerzos
EventoRefuerzosSolicitados { RefuerzosDisponibles, CantidadSolicitada, PosicionCombate }
EventoRefuerzoProgramado { Personaje, TurnoLlegada, TurnosRestantes }
EventoRefuerzoLlegado { Personaje, TurnoLlegada }
EventoRefuerzosCancelados { RefuerzosCancelados }
```

---

## Integración con Otros Sistemas

### PlayerInterestZone

La zona de detección sigue automáticamente al main:

```csharp
[SerializeField] private bool followMainCharacter = true;

// Se suscribe a cambios de main
PlayerPartyManager.Instance.OnMainChanged += OnMainCharacterChanged;
```

### CombatEncounterManager

Obtiene el party dinámicamente:

```csharp
[SerializeField] private bool usePlayerPartyManager = true;

// PartyMembers devuelve el party activo automáticamente
public IReadOnlyList<EntityController> PartyMembers => 
    usePlayerPartyManager ? partyManager.ActiveParty : manualPartyMembers;
```

### EntityController

Flag para identificar personajes del jugador:

```csharp
public bool IsPlayerOwned { get; }
public void SetPlayerOwned(bool owned);  // Llamado por PlayerPartyManager
```

---

## Configuración en Unity

### Setup del PlayerPartyManager

1. **Crear GameObject** con `PlayerPartyManager`
2. Marcar como **DontDestroyOnLoad** (se hace automáticamente)
3. Configurar límites si es necesario

### Setup del ReinforcementSystem

1. **Agregar componente** al mismo objeto o separado
2. Configurar `distancePerTurn` según escala del mundo
3. Habilitar `autoProcessOnTurnStart`

### Registrar Personajes Iniciales

```csharp
void Start()
{
    var manager = PlayerPartyManager.Instance;
    
    // Registrar personaje principal
    manager.RegisterCharacter(protagonista);  // Automáticamente es main
    
    // Registrar compañeros iniciales
    manager.RegisterCharacter(companero1);
    manager.AddToActiveParty(companero1);
    
    // Registrar personaje estacionado
    manager.RegisterCharacter(mercenario);
    manager.StationCharacter(mercenario, "Taberna");
}
```

---

## Ejemplos de Uso

### Cambiar de Personaje (Switch)

```csharp
public void OnSwitchCharacterPressed(EntityController target)
{
    if (PlayerPartyManager.Instance.IsInActiveParty(target))
    {
        PlayerPartyManager.Instance.SetMainCharacter(target, keepOldMainInParty: true);
    }
}
```

### Mostrar UI de Refuerzos

```csharp
public void ShowReinforcementUI(Vector3 combatPos)
{
    var disponibles = PlayerPartyManager.Instance.GetAvailableReinforcements(combatPos);
    
    foreach (var r in disponibles)
    {
        Debug.Log($"{r.Character.Nombre_Entidad}: {r.Distance}u, {r.TurnsToArrive} turnos");
        // Crear botón en UI para llamar a cada uno
    }
}
```

### Listener de Eventos de Party

```csharp
void OnEnable()
{
    EventBus.Suscribir<EventoMainCambiado>(OnMainCambiado);
    EventBus.Suscribir<EventoRefuerzoLlegado>(OnRefuerzoLlego);
}

void OnMainCambiado(EventoMainCambiado e)
{
    // Actualizar cámara, UI, etc.
    CameraController.Instance.SetTarget(e.NuevoMain.transform);
}

void OnRefuerzoLlego(EventoRefuerzoLlegado e)
{
    // Mostrar notificación
    UIManager.ShowNotification($"¡{e.Personaje.Nombre_Entidad} llegó al combate!");
}
```

---

## Debug

### PlayerPartyManager

```csharp
[ContextMenu("Debug: Mostrar Estado")]
void DebugShowState()
// Muestra main, party activo, estacionados, etc.
```

### ReinforcementSystem

```csharp
[ContextMenu("Debug: Mostrar Refuerzos Pendientes")]
void DebugShowPending()
// Muestra refuerzos en camino y turnos restantes
```
