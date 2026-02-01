# Sistema de Combate

## Visión General

El sistema de combate en Saclisam es **por turnos** con detección **dinámica de enemigos** basada en proximidad. Los enemigos entran automáticamente al combate cuando cumplen ciertas condiciones, sin necesidad de asignación manual.

```
┌─────────────────────────────────────────────────────────────────┐
│                    FLUJO DE COMBATE                             │
│                                                                 │
│  ┌──────────────────┐    ┌─────────────────────┐               │
│  │ PlayerInterestZone│───►│CombatEncounterManager│              │
│  │  (Detección)      │    │   (Orquestación)    │              │
│  └──────────────────┘    └──────────┬──────────┘              │
│                                     │                          │
│                          ┌──────────▼──────────┐              │
│                          │   CombateManager    │              │
│                          │ (Ejecución turnos)  │              │
│                          └──────────┬──────────┘              │
│                                     │                          │
│                          ┌──────────▼──────────┐              │
│                          │    TurnManager      │              │
│                          │ (Orden de turnos)   │              │
│                          └─────────────────────┘              │
└─────────────────────────────────────────────────────────────────┘
```

---

## Componentes Principales

### 1. PlayerInterestZone

**Archivo**: `Assets/Scripts/Managers/PlayerInterestZone.cs`

Zona de detección que sigue al personaje principal. Detecta enemigos potenciales y los reporta al EncounterManager.

```csharp
[Header("Configuración de Rangos")]
[SerializeField] private float detectionRadius = 20f;     // Detectar
[SerializeField] private float engagementRadius = 10f;    // Iniciar combate
[SerializeField] private float updateInterval = 0.2f;     // Frecuencia

[Header("Seguimiento del Main")]
[SerializeField] private bool followMainCharacter = true; // Sigue al main dinámicamente
```

#### Eventos Publicados

| Evento | Descripción |
|--------|-------------|
| `EventoCandidatoDetectado` | Enemigo entró en zona de detección |
| `EventoCandidatoEnRangoCombate` | Enemigo listo para combate |
| `EventoCandidatoSalioRangoCombate` | Enemigo salió del rango |

#### Integración con PlayerPartyManager

```csharp
// Se suscribe a cambios de main para actualizar qué personaje sigue
PlayerPartyManager.Instance.OnMainChanged += OnMainCharacterChanged;
```

---

### 2. CombatEncounterManager

**Archivo**: `Assets/Scripts/Managers/CombatEncounterManager.cs`

Manager central que decide **qué enemigos entran en combate** basándose en reglas configurables.

#### Propiedades

```csharp
public bool CombatInProgress { get; }
public IReadOnlyList<ICombatCandidate> EnemiesInCombat { get; }
public IReadOnlyList<EntityController> PartyMembers { get; }  // Dinámico
public CombatRules Rules { get; }
```

#### Configuración

```csharp
[Header("Integración")]
[SerializeField] private bool usePlayerPartyManager = true;  // Usar party dinámico
[SerializeField] private List<EntityController> manualPartyMembers;  // Fallback manual
```

#### Flujo de Evaluación

```
1. PlayerInterestZone detecta enemigo en rango
       ↓
2. OnCandidateInEngagementRange() recibe el candidato
       ↓
3. EvaluateEncounter() evalúa condiciones:
   - ¿Cooldown de encuentro pasó?
   - ¿Candidato puede unirse? (CanJoinCombat)
   - ¿Hay línea de visión? (si requerido)
   - ¿Diferencia de nivel aceptable?
       ↓
4. PrioritizeAndLimit() ordena por:
   - Distancia, Nivel, Prioridad, o Aleatorio
       ↓
5. StartEncounter() o AddEnemiesToCombat()
```

---

### 3. CombatRules (ScriptableObject)

**Archivo**: `Assets/Scripts/Managers/CombatRules.cs`

Configuración de reglas de combate en un asset reutilizable.

```csharp
[Header("Límites de Combate")]
public int maxEnemiesPerEncounter = 5;
public int maxAlliesPerEncounter = 4;
public int minAlliesRequired = 1;

[Header("Condiciones de Inicio")]
public bool autoStartCombat = true;
public float encounterCooldown = 3f;
public bool requireLineOfSight = false;
public int maxLevelDifference = 10;

[Header("Priorización")]
public EnemyPrioritization prioritization = EnemyPrioritization.ByDistance;
public bool prioritizeAggro = true;
```

#### Tipos de Priorización

| Tipo | Descripción |
|------|-------------|
| `ByDistance` | Más cercanos primero |
| `ByLevel` | Nivel más alto primero |
| `ByLevelAscending` | Nivel más bajo primero |
| `ByPriority` | Por prioridad de aggro |
| `Random` | Aleatorio |

---

### 4. CombateManager

**Archivo**: `Assets/Scripts/Managers/CombateManager.cs`

Ejecuta el combate por turnos una vez iniciado.

#### Métodos Principales

```csharp
// Iniciar combate dinámico (nuevo sistema)
void IniciarCombateConEntidades(List<EntityController> party, List<EnemyController> enemigos)

// Agregar entidades durante combate
void AgregarEnemigosAlCombate(List<EnemyController> nuevosEnemigos)
void AgregarAliadoAlCombate(EntityController aliado)  // Para refuerzos

// Modo legacy (asignación manual)
void IniciarCombate()  // Usa listas asignadas en inspector
```

#### Configuración

```csharp
[Header("Modo de Operación")]
[SerializeField] private bool useLegacyMode = false;  // true = manual, false = dinámico
```

---

### 5. ICombatCandidate (Interface)

**Archivo**: `Assets/Scripts/Interfaces/ICombatCandidate.cs`

Interface que deben implementar las entidades que pueden entrar en combate dinámicamente.

```csharp
public interface ICombatCandidate
{
    string CandidateId { get; }
    Transform CandidateTransform { get; }
    int CombatPriority { get; }  // Mayor = más agresivo
    
    bool CanJoinCombat(CombatContext context);
    void OnSelectedForCombat();
    void OnRemovedFromCombat();
}
```

#### CombatContext

```csharp
public class CombatContext
{
    public bool CombatInProgress;
    public int CurrentEnemyCount;
    public int PartyAliveCount;
    public int PartyAverageLevel;
    public Vector3 PlayerPosition;
}
```

---

## Eventos del Sistema de Combate

### Eventos de Detección/Encuentro

```csharp
// Candidato detectado en rango
EventoCandidatoDetectado { Candidato, EnRangoEngagement }

// Candidato en rango de combate
EventoCandidatoEnRangoCombate { Candidato }

// Encuentro iniciado
EventoEncounterIniciado { Party, Enemigos }

// Enemigos agregados mid-combat
EventoEnemigosAgregados { NuevosEnemigos }
```

### Eventos de Combate

```csharp
// Combate iniciado
EventoCombateIniciado { Jugadores, Enemigos }

// Turno
EventoTurnoIniciado { Entidad, NumeroTurno }
EventoTurnoFinalizado { Entidad }

// Combate terminado
EventoCombateFinalizado { Victoria, XPGanada, OroGanado }
```

---

## Flujo Completo de Combate

```
1. EXPLORACIÓN
   └── PlayerInterestZone detecta enemigos en rango

2. EVALUACIÓN
   └── CombatEncounterManager evalúa condiciones
       └── Filtra por reglas (CombatRules)
       └── Prioriza candidatos

3. INICIO
   └── CombateManager.IniciarCombateConEntidades()
       └── Registra party y enemigos
       └── Inicializa TurnManager
       └── Publica EventoCombateIniciado

4. TURNOS (loop)
   └── TurnManager ordena por velocidad
   └── Por cada turno:
       ├── Procesar estados (veneno, etc.)
       ├── Obtener acción (jugador: UI / enemigo: IA)
       └── Ejecutar acción

5. REFUERZOS (opcional)
   └── Nuevos enemigos detectados → AgregarEnemigosAlCombate()
   └── Aliados llamados → AgregarAliadoAlCombate()

6. FIN
   └── Todos enemigos derrotados → Victoria
   └── Todo party derrotado → Derrota
   └── Publicar EventoCombateFinalizado
```

---

## Configuración en Unity

### Setup Mínimo

1. **Crear CombatRules asset**:
   - `Assets/Create/Combat/Combat Rules`
   - Configurar límites y condiciones

2. **Agregar PlayerInterestZone**:
   - Añadir a objeto que siga al jugador
   - O habilitar `followMainCharacter = true`

3. **Agregar CombatEncounterManager**:
   - Crear GameObject con el componente
   - Asignar CombatRules
   - Habilitar `usePlayerPartyManager = true`

4. **Enemigos con ICombatCandidate**:
   - EnemyController ya implementa ICombatCandidate
   - Configurar `combatPriority` si es necesario

### Debug

```csharp
// En CombatEncounterManager
[ContextMenu("Debug: Mostrar Estado")]
void DebugShowState()  // Muestra estado actual

[ContextMenu("Debug: Forzar Evaluación")]
void DebugForceEvaluate()  // Fuerza evaluación ignorando cooldown
```

---

## Ejemplos de Uso

### Forzar Inicio de Combate

```csharp
// Desde script
var party = PlayerPartyManager.Instance.ActiveParty.ToList();
var enemigos = GetNearbyEnemies();
CombateManager.Instance.IniciarCombateConEntidades(party, enemigos);
```

### Suscribirse a Eventos de Combate

```csharp
void OnEnable()
{
    EventBus.Suscribir<EventoCombateIniciado>(OnCombateIniciado);
    EventBus.Suscribir<EventoCombateFinalizado>(OnCombateFinalizado);
}

void OnCombateIniciado(EventoCombateIniciado e)
{
    Debug.Log($"Combate! {e.Jugadores.Count} vs {e.Enemigos.Count}");
}
```

### Crear Enemigo que Entra al Combate

```csharp
// El EnemyController ya implementa ICombatCandidate
public class EnemyController : MonoBehaviour, ICombatCandidate
{
    public bool CanJoinCombat(CombatContext context)
    {
        // Lógica custom de cuándo puede entrar
        return EstaVivo() && !context.CombatInProgress; // Solo si no hay combate
    }
}
```
