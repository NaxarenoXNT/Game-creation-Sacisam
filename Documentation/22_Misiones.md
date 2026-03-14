# Sistema de Misiones

> Sistema de misiones multi-personaje con tres scopes: **Global**, **Personal** y **Exclusiva**.

---

## 1. Objetivo del Sistema

Diseñar un sistema de misiones donde:

* Cada zona, ciudad, facción, religión y NPC tenga su propia narrativa.
* El mundo avance con o sin la intervención del jugador.
* Las decisiones tengan consecuencias permanentes.
* Cada **personaje** tenga su propio estado de misiones (per-character).
* Existan misiones **globales** (compartidas), **personales** (exclusivas del personaje) y **exclusivas** (globales hasta ser reclamadas).

---

## 2. Principios de Diseño

1. **Data-driven**: las misiones son ScriptableObjects, no lógica procedural.
2. **Per-character evaluation**: las condiciones se evalúan contra el `EvolutionState` de cada personaje.
3. **Tres scopes**: Global, Personal, Exclusive — con reglas distintas de progreso y recompensas.
4. **Event-driven**: el `MissionManager` no usa Update; reacciona a eventos del EventBus.
5. **Separación de responsabilidades**: mundo, misiones, NPCs y consecuencias desacoplados.
6. **Pérdida real de contenido**: no todo es salvable.

---

## 3. Tipos de Misión (MissionScope)

### Global
- Atadas a facciones, ciudades, NPCs del mundo.
- **Cualquier personaje** del jugador puede contribuir progreso.
- Se evalúa si ALGÚN personaje registrado cumple las condiciones de desbloqueo.
- Al completarse, se registra en `GlobalPlayerState.misionesGlobalesCompletadas`.
- Se registra también en el `EvolutionState` de TODOS los personajes (para que cadenas funcionen).
- Recompensas → cuenta del jugador (global).

### Personal
- Únicas por personaje: se desbloquean por poseer una clase, evolución, trait o nivel específico.
- Solo el personaje que la desbloqueó puede progresar y completarla.
- Se registra en `CharacterMissionData` del personaje.
- Se registra en `EvolutionState` del personaje que la completó.
- Recompensas → al personaje específico.

### Exclusive (Exclusiva)
- Comienzan como globales (disponibles para todos).
- Cuando un personaje la **acepta**, queda asignada permanentemente a ese personaje.
- La asignación se registra en `GlobalPlayerState.misionesExclusivasAsignadas`.
- Una vez asignada, se trata como personal (solo ese personaje puede progresar).
- Ningún otro personaje puede reclamarla.

---

## 4. Arquitectura

### MissionDefinitionSO (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "Missions/Mission Definition")]
public class MissionDefinitionSO : ScriptableObject
{
    public string misionId;
    public string nombreMostrar;
    public string descripcion;
    public MissionScope scope;              // Global, Personal, Exclusive
    public bool autoAceptar;                // ¿Se acepta automáticamente al estar disponible?

    // Condiciones de desbloqueo — evaluadas contra EvolutionState del personaje
    public List<MissionConditionSO> condicionesDesbloqueo;

    // Objetivos — instanciados como MissionObjectiveInstance en runtime
    public List<MissionObjectiveSO> objetivos;

    // Recompensas
    public List<MissionRewardSO> recompensas;

    // Cadenas de misiones
    public MissionDefinitionSO siguienteMisionEnCadena;

    public bool CumpleCondicionesDesbloqueo(EvolutionState state)
    {
        foreach (var cond in condicionesDesbloqueo)
            if (!cond.Evaluar(state)) return false;
        return true;
    }
}
```

### MissionConditionSO (condiciones de desbloqueo)

Patrón idéntico a `EvolutionConditionSO` — cada condición evalúa contra `EvolutionState`.

```csharp
public abstract class MissionConditionSO : ScriptableObject
{
    public abstract bool Evaluar(EvolutionState state);
    public abstract float GetProgreso(EvolutionState state);
    public abstract string GetDescripcionAuto();
}
```

**Tipos implementados:**

| SO | Evalúa |
|----|--------|
| `MissionCompletedConditionSO` | `state.misionesCompletadas.Contains(misionId)` |
| `LevelMissionConditionSO` | `state.nivelJugador >= nivelRequerido` |
| `KarmaMissionConditionSO` | `state.karma` rango |
| `HasTraitMissionConditionSO` | `state.traitStacks.ContainsKey(traitId)` |
| `FlagMissionConditionSO` | `state.customFlags[flagKey] >= valorRequerido` |

### MissionObjectiveSO (objetivos)

```csharp
public abstract class MissionObjectiveSO : ScriptableObject
{
    public string objectiveId;
    public string descripcion;
    public bool esObligatorio = true;
    public abstract ObjectiveInstance CrearInstancia();
}
```

**Tipos**: `KillObjectiveSO`, `CollectItemObjectiveSO`, `ReachZoneObjectiveSO`, `InteractObjectiveSO`.

### MissionInstance (runtime)

```csharp
public class MissionInstance
{
    public MissionDefinitionSO definition;
    public MissionStatus status;          // Active, Completed, Failed
    public List<ObjectiveInstance> objetivos;
    public float tiempoAceptada;
}
```

---

## 5. MissionManager (Orquestador)

**Archivo**: `Assets/Scripts/Missions/MissionManager.cs`

MonoBehaviour 100% event-driven (sin Update).

### Responsabilidades
- Gestiona misiones de los tres scopes.
- Registra personajes con su `EvolutionState`.
- Enruta eventos (kills, traits, items, zonas) al scope correcto.
- Previene duplicados y regula estados compartidos vía `GlobalPlayerState`.

### Inicialización

```csharp
// Se inicializa con estado global
missionManager.Inicializar(globalPlayerState);

// Cada personaje se registra con su estado individual
missionManager.RegistrarPersonaje(characterId, evolutionState);
```

### Eventos Suscritos

```csharp
EventBus.Suscribir<EventoMuerte>(HandleMuerte);               // Kills → misiones personales + globales
EventBus.Suscribir<EventoNivelSubido>(HandleNivelSubido);     // Re-evaluar todo
EventBus.Suscribir<EventoTraitObtenido>(HandleTraitObtenido); // Registrar en global + re-evaluar
EventBus.Suscribir<EventoEvolucionAplicada>(HandleEvolucion);
EventBus.Suscribir<EventoMisionCompletada>(HandleCadena);     // Desbloquea misiones encadenadas
```

### Participantes de Combate

Cuando un enemigo muere, **todos los personajes del party activo** reciben crédito para sus misiones personales. Misiones globales también reciben el kill.

```
Personaje A: "Mata 10 goblins" → progreso 6/10
Personaje B: "Mata 10 goblins" → progreso 1/10
→ Combat kill: 3 goblins
Personaje A: → 9/10
Personaje B: → 4/10
```

### API

```csharp
bool AceptarMisionGlobal(string misionId);
bool AceptarMisionPersonal(string misionId, string characterId);
bool AceptarMisionExclusiva(string misionId, string characterId);
bool FallarMision(string misionId, string characterId, string razon);
bool FallarMisionGlobal(string misionId, string razon);

// Notificaciones externas
void NotificarZonaAlcanzada(string zonaId, string characterId);
void NotificarItemObtenido(string itemId, int cantidad, string characterId);
void ForzarRevaluacion();

// Consulta
IReadOnlyDictionary<string, MissionInstance> GetMisionesActivasPersonaje(string charId);
IReadOnlyDictionary<string, MissionInstance> GetMisionesGlobalesActivas();
bool EsMisionCompletada(string misionId, string characterId = null);
```

---

## 6. Persistencia

### MissionSaveData

```csharp
public class MissionSaveData
{
    // Globales
    public List<string> globalesCompletadas;
    public List<string> globalesFallidas;
    public List<MissionExclusiveAssignment> exclusivasAsignadas;
    public List<MissionActiveSaveData> globalesActivas;

    // Per-personaje
    public List<CharacterMissionSaveData> datosPersonajes;
}
```

El `MissionManager` expone `ObtenerDatosGuardado()` y `CargarDatosGuardado()`.

---

## 7. CharacterMissionData (per-personaje)

```csharp
public class CharacterMissionData
{
    public string characterId;
    public Dictionary<string, MissionInstance> misionesActivas;
    public HashSet<string> misionesCompletadas;
    public HashSet<string> misionesFallidas;
    public HashSet<string> misionesDisponibles;
}
```

---

## 8. Eventos Publicados

| Evento | Cuándo |
|--------|--------|
| `EventoMisionDisponible` | Misión cumple condiciones de desbloqueo |
| `EventoMisionAceptada` | Misión aceptada (cualquier scope) |
| `EventoMisionProgreso` | Progreso en un objetivo |
| `EventoObjetivoCompletado` | Un objetivo individual completado |
| `EventoMisionCompletada` | Misión terminada exitosamente |
| `EventoMisionFallida` | Misión fallida |

---

## 9. Flujo Completo

```
1. Juego → Evento (kill, nivel, trait, etc.)
           ↓
2. EventBus → MissionManager.Handle*()
           ↓
3. MissionManager enruta al scope correcto:
   - Kill → misiones personales de cada participante + globales
   - Trait → re-evaluar personaje + globales
   - Nivel → re-evaluar todo
           ↓
4. RevaluarMisiones():
   - Global: ¿ALGÚN personaje cumple condiciones? → disponible
   - Personal: ¿ESTE personaje cumple? → disponible para él
   - Exclusive: si no asignada → global; si asignada → personal del dueño
           ↓
5. Jugador acepta misión:
   - Global: AceptarMisionGlobal()
   - Personal: AceptarMisionPersonal(charId)
   - Exclusive: AceptarMisionExclusiva(charId) → bloquea a ese personaje
           ↓
6. Progreso → VerificarCompletitud()
           ↓
7. Si completa:
   - Global: registra en GlobalPlayerState + EvolutionState de TODOS
   - Personal: registra en CharacterMissionData + EvolutionState del pj
           ↓
8. EventoMisionCompletada → puede desbloquear siguientes misiones en cadena
```

---

## 10. Integración con Evoluciones

- `MisionConditionSO` (en sistema de evoluciones) evalúa `state.misionesCompletadas` — funciona porque `MissionManager` registra misiones completadas en el `EvolutionState`.
- Misiones globales completadas se propagan a TODOS los `EvolutionState`, asegurando que traits que requieren misiones globales se evalúen correctamente para cualquier personaje.
- Misiones personales solo se registran en el `EvolutionState` del personaje que las completó.

---

## 11. Comparación con Sistema de Traits

| Evoluciones/Traits | Misiones |
|---------------------|----------|
| `EvolutionConditionSO` | `MissionConditionSO` |
| `TraitDefinition` | `MissionDefinitionSO` |
| `TraitChainDefinition` | `MissionDefinitionSO.siguienteMisionEnCadena` |
| `EvolutionState` | `EvolutionState` (compartido) |
| `GlobalPlayerState` | `GlobalPlayerState` (compartido) |
| Evalúa: per-character | Evalúa: per-character |
