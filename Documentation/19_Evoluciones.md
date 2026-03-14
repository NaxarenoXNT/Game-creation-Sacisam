# Sistema de Evoluciones y Traits

Sistema data-driven para evoluciones de clase y traits, evaluado **por personaje**.

## Filosofía del Sistema

**Traits desbloquean Evoluciones:**
- Los **TRAITS** se desbloquean cumpliendo **condiciones genéricas** (kills, karma, misiones, etc.)
- Las **EVOLUCIONES** se desbloquean teniendo los **traits requeridos**
- Esto permite que los traits actúen como "logros" que habilitan opciones de evolución
- Permite crear evoluciones complejas sin hardcodear condiciones específicas

**Las evoluciones no reemplazan la clase base, la aumentan:**
- Al evolucionar se inyecta un `IComportamientoDeClase` (módulo) sobre el objeto existente
- El objeto `Guerrero` sigue siendo `Guerrero`; el módulo `PaladinModulo` se apila encima
- Las evoluciones posteriores también se apilan; la iteración determina qué gana
- Ver `03_Clases_Jugador.md` para la documentación completa del sistema de módulos

**Evaluación por personaje (Multi-Personaje):**
- Cada personaje tiene su propio `EvolutionState` con contadores individuales (kills, karma, traits, etc.)
- Las condiciones se evalúan con `Evaluar(EvolutionState)` — reciben el estado del personaje específico
- Un trait desbloqueado por un personaje NO se desbloquea automáticamente para otro
- Las evoluciones solo se evalúan contra el personaje que tiene los traits requeridos
- Los traits marcados como `globalmenteUnico` en `TraitDefinition` se registran en `GlobalPlayerState` y NO pueden ser desbloqueados por otro personaje

## Objetivos
- Evoluciones exclusivas por clase base; traits aplicables a casi todas las clases
- Desbloqueo modular: traits por condiciones → evoluciones por traits
- Sistema extensible: agregar nuevas condiciones sin modificar código
- Ofertas ponderadas para reducir runs repetitivas
- Evaluación individual por personaje con registro global de unicidad

---

## ScriptableObjects

### EvolutionConditionSO (condición como ScriptableObject)
Cada condición es un **SO independiente y reutilizable**. Usada por `TraitDefinition` y `TraitChainDefinition` para requisitos de desbloqueo.

```csharp
// Clase base abstracta
public abstract class EvolutionConditionSO : ScriptableObject
{
    public string descripcionUI;
    public Sprite icono;

    public abstract bool Evaluar(EvolutionState state);          // ¿Se cumple para este personaje?
    public abstract float GetProgreso(EvolutionState state);     // 0.0 a 1.0
    public abstract string GetDescripcionAuto();                 // Texto auto-generado para UI
    public virtual bool EsEscalable => false;                    // Para cadenas
}
```

**Tipos de Condiciones disponibles (14 SOs concretos):**

| SO Concreto | Menú de Creación | Campos | Evalúa |
|-------------|------------------|--------|--------|
| `KillsConditionSO` | Conditions/Kills Tipo | `tipoEntidad`, `cantidad` | `state.killsPorTipo` |
| `KillsTotalConditionSO` | Conditions/Kills Total | `cantidad` | `state.killsPorTipo.Values.Sum()` |
| `KarmaConditionSO` | Conditions/Karma | `comparacion`, `valor`, `valorMax` | `state.karma` |
| `TraitConditionSO` | Conditions/Tiene Trait | `traitRequerido`/`traitId` | `state.traitStacks` |
| `NivelConditionSO` | Conditions/Nivel Minimo | `nivelMinimo` | `state.nivelJugador` |
| `SacrificiosConditionSO` | Conditions/Sacrificios | `cantidad` | `state.sacrificios` |
| `MisionConditionSO` | Conditions/Mision Completada | `misionId` | `state.misionesCompletadas` |
| `EstadoConditionSO` | Conditions/Estado Aplicado | `estado`, `vecesAplicado` | `state.estadosAplicados` |
| `DanoInfligidoConditionSO` | Conditions/Daño Infligido | `cantidad` | `state.dañoInfligidoTotal` |
| `CuracionConditionSO` | Conditions/Curación Total | `cantidad` | `state.curacionTotal` |
| `HabilidadUsadaConditionSO` | Conditions/Habilidad Usada | `habilidadId`, `veces` | `state.usosHabilidad` |
| `PoseeHabilidadConditionSO` | Conditions/Posee Habilidad | `habilidadId` | `state.habilidadesDesbloqueadas` |
| `CustomConditionSO` | Conditions/Custom Flag | `flagKey`, `valorMinimo` | `state.customFlags` |

### TraitDefinition (cross-class)
Se desbloquea cumpliendo **condiciones genéricas**, evaluadas por personaje.

```csharp
// Identidad
string id;
string nombreMostrar;
string descripcion;
Sprite icono;
EvolutionRarity rareza;
float pesoOferta;
bool visible;
string hintOculto;

// Restricciones
List<ClaseData> clasesBloqueadas;  // Clases que NO pueden obtenerlo
bool stackeable;
int maxStacks;
bool globalmenteUnico;  // Si true, solo UN personaje puede tenerlo (GlobalPlayerState)

// Desbloqueo — List<EvolutionConditionSO>, evalúa contra EvolutionState del personaje
List<EvolutionConditionSO> condiciones;  // TODAS deben cumplirse
List<TraitDefinition> exclusiones;       // Traits mutuamente excluyentes

// Efectos
List<EvolutionEffect> efectos;
```

> **Nota Multi-Personaje:** Un trait con `globalmenteUnico = true` se registra en `GlobalPlayerState.traitsGlobalmenteBloqueados` con el `characterId` que lo desbloqueó.

### ClassEvolutionDefinition (exclusiva por clase)
Se desbloquea teniendo los **traits requeridos**.

```csharp
// Identidad
string id;
string nombreMostrar;
string descripcion;
Sprite icono;

// Clase
ClaseData claseOrigen;   // Clase requerida (referencia SO)
ClaseData claseDestino;  // Nueva clase al evolucionar
int tier;                // 1=básica, 2=avanzada, 3=legendaria

// Presentación
EvolutionRarity rareza;
float pesoOferta;
bool visible;
string hintOculto;

// Requisitos
List<TraitDefinition> traitsRequeridos;  // TRAITS, no condiciones
int nivelMin;  // Único requisito directo (universal)

// Exclusiones
List<ClassEvolutionDefinition> exclusiones;

// Efectos — incluyendo AgregarModulo para inyectar comportamiento de clase
List<EvolutionEffect> efectos;
```

### EvolutionEffect (efecto atómico)

```csharp
EvolutionEffectType tipo;  // Ver tabla abajo
TargetStat stat;
float valor;
HabilidadData habilidad;   // Para AddAbility
PasivaData pasiva;         // Para AddPassive
ModuloClaseSO moduloSO;    // Para AgregarModulo  ← NUEVO
```

**Tipos de Efectos:**
| Tipo | Descripción |
|---|---|
| `AddStatFlat` | Incremento plano de stat |
| `AddStatPercent` | Incremento porcentual de stat |
| `AddAbility` | Agrega habilidad activa |
| `AddPassive` | Agrega habilidad pasiva |
| `RemoveAbility` | Remueve habilidad activa |
| `RemovePassive` | Remueve habilidad pasiva |
| `AgregarModulo` | **Inyecta un módulo de comportamiento de clase** |
| `ModifyCooldowns` | Ajusta cooldowns |
| `AddElement` | Agrega elemento |
| `AddStatusPassive` | Aplica status pasivo persistente |
| `KarmaDelta` | Modifica karma |
| `ReputationDelta` | Modifica reputación de facción |
| `WorldRuleToggle` | Activa regla de mundo |
| `AITargetBias` | Ajusta bias de IA |
| `LootTableBias` | Ajusta peso en tablas de drop |
| `TagAdd` | Agrega tag al EvolutionState |

### ModuloClaseSO (factory de módulos de evoluión)

SO abstracto. Cada módulo concreto tiene su propio SO derivado.

```
Assets/Scripts/Subclases/Modulos/
├── IComportamientoDeClase.cs   ← contrato de hooks
├── ModuloClaseSO.cs            ← SO abstracto (factory)
├── PaladinModuloSO.cs          ← Create > Clases/Modulos/Paladin
└── HeraldoCaidoModuloSO.cs     ← Create > Clases/Modulos/Heraldo Caido
```

**Modules disponibles:**

| Módulo | ID | Hooks activos |
|---|---|---|
| Paladín | `paladin` | Curación ±20%, None→Light, +20% vs Undead |
| Heraldo Caído | `heraldo_caido` | None→Dark (pisa Light del Paladín) |

### EvolutionBranch (árbol por clase)
```csharp
ClaseData claseOrigen;
List<ClassEvolutionDefinition> evoluciones;
List<TraitDefinition> traitsRelacionados;
```

---

## Componentes del Sistema

### EvolutionState
Estado runtime completo para evaluación:
- Contadores: kills, daño, curación, usos de items/habilidades
- Progresión: nivel, karma, reputaciones, rangos de facción
- Exploración: biomas visitados, tiempo jugado
- Traits y evoluciones obtenidas
- Custom flags para condiciones especiales (ej: `customFlags["union_iglesia"]`)

### EvolutionEvaluator
Evalúa disponibilidad:
1. **Traits**: Cumple TODAS las condiciones genéricas + clase no bloqueada + no excluido
2. **Evoluciones**: Tiene TODOS los traits requeridos + clase correcta + nivel mínimo

### EvolutionRoller
Genera ofertas ponderadas (2-3 opciones) usando `pesoOferta`.

### EvolutionApplier
Aplica `EvolutionEffect` al jugador. Para `AgregarModulo` llama
`efecto.moduloSO.Instanciar()` y luego `jugador.AgregarModulo(modulo)`.

### EvolutionController (MonoBehaviour)
- Se suscribe al EventBus
- Actualiza EvolutionState
- Genera ofertas
- Expone API para UI
- Integra con SaveSystem

---

## Flujo

```
1. JUEGO → Eventos (kill, misión, karma, etc.)
           ↓
2. EventBus → EvolutionController.Handle*()
           ↓
3. EvolutionState se actualiza
           ↓
4. UI solicita GenerarOferta()
           ↓
5. EvolutionEvaluator filtra disponibles:
   - Traits: CumpleCondiciones()
   - Evoluciones: TieneTraitsRequeridos()
           ↓
6. EvolutionRoller pondera y selecciona 2-3
           ↓
7. UI muestra opciones
           ↓
8. Jugador elige → AplicarOpcion()
           ↓
9. EvolutionApplier aplica efectos:
   → AddStatFlat/Percent → modifica stats
   → AddAbility/Passive  → GestorHabilidades/GestorPasivas
   → AgregarModulo       → jugador.AgregarModulo(moduloSO.Instanciar())
           ↓
10. SaveSystem persiste estado
```

---

## Ejemplos de Diseño

### Ejemplo: Ruta del Paladín (Guerrero → Paladín)

**Requisitos configurados en la `ClassEvolutionDefinition`:**
```yaml
claseOrigen: Guerrero
claseDestino: Guerrero   # el objeto no cambia de tipo, el módulo lo transforma
nivelMin: 20
traitsRequeridos:
  - cazador_nomuertos_ii   # completa la cadena de traits Cazador de No-Muertos
  - union_iglesia          # misión: unirse a la Iglesia (customFlag)
efectos:
  - tipo: AgregarModulo
    moduloSO: PaladinModuloSO   # ← arrastar el SO desde Resources
```

**Qué hace el `PaladinModulo` al agregarse:**
- Registra `PaladinDamageMod` (IDamageModifier, Order 350) en `EntityDamageModifiers`
  → +20% daño físico y elemental vs entidades Undead
- `ModificarElementoAtaque`: si el ataque no tiene elemento → Light
- `ModificarCuracionOtorgada` / `ModificarCuracionRecibida`: +20% (aditivo)
- `OverridearRecursoPrincipal`: retorna null por ahora (Fe pendiente de diseño)

**Trait: Cazador de No-Muertos (cadena)**
```yaml
condicionesBase:
  - tipo: KillsTipo, parametro: "Undead", cantidad: 50
nodos:
  - sufijo: "I",  efectos: [TagAdd: cazador_nomuertos_i]
  - sufijo: "II", efectos: [TagAdd: cazador_nomuertos_ii]
```

### Ejemplo: Ruta del Heraldo Caído (Paladín → Heraldo Caído)

```yaml
claseOrigen: Guerrero   # sigue siendo Guerrero en el sistema de clases
nivelMin: 35
traitsRequeridos:
  - pacto_oscuro
  - traidor_de_la_fe
efectos:
  - tipo: AgregarModulo
    moduloSO: HeraldoCaidoModuloSO
```

**Resultado en runtime (lista de módulos del jugador):**
```
[ PaladinModulo, HeraldoCaidoModulo ]

Elemento de ataque (sustitutivo, reversa):
  HeraldoCaidoModulo → Dark  ← gana, el Paladín no se consulta

Curación recibida (aditivo, normal):
  PaladinModulo: base × 1.20
  HeraldoCaidoModulo: sin cambio
  Resultado: +20% del Paladín se mantiene
```

### Cómo agregar un NPC que ya arranca como Paladín

1. En el inspector del `ClaseData` del NPC, buscar el campo `modulosIniciales`
2. Agregar el `PaladinModuloSO` a la lista
3. `ClaseData.CrearInstancia()` llama `jugador.AgregarModulo()` automáticamente

---

## Cómo agregar contenido sin código

### Nuevo Trait
1. `Create > Evolutions > Trait`
2. Definir id, nombre, descripción, icono
3. Añadir condiciones (lista de EvolutionCondition)
4. Añadir efectos

### Nueva Evolución
1. `Create > Evolutions > ClassEvolution`
2. Asignar `claseOrigen` (referencia al ClaseData base)
3. Añadir `traitsRequeridos` y `nivelMin`
4. En `efectos`, agregar un efecto tipo `AgregarModulo` con el SO del módulo

### Nuevo Módulo de Clase (requiere código)
1. Crear `MiEvolucionModulo.cs` implementando `IComportamientoDeClase`
2. Crear `MiEvolucionModuloSO.cs` extendiendo `ModuloClaseSO` con `[CreateAssetMenu]`
3. En `Instanciar()`, retornar `new MiEvolucionModulo(parámetros del SO)`
4. Crear el asset SO en Unity
5. Arrastrarlo al efecto `AgregarModulo` de la evolución correspondiente

### Nueva Condición (requiere código)
1. Crear nuevo SO concreto heredando de `EvolutionConditionSO`
2. Implementar `Evaluar(EvolutionState)`, `GetProgreso()`, `GetDescripcionAuto()`
3. Añadir `[CreateAssetMenu]` para crearlo desde el Inspector
4. Añadir campo correspondiente en `EvolutionState` si es necesario
5. Conectar evento en `EvolutionController` para actualizar el estado

---

## Hooks al EventBus

El `EvolutionController` se suscribe a estos eventos para actualizar el `EvolutionState` del personaje correspondiente:

```csharp
EventBus.Suscribir<EventoHabilidadUsada>(HandleHabilidadUsada);
EventBus.Suscribir<EventoMuerte>(HandleMuerte);                   // actualiza killsPorTipo
EventBus.Suscribir<EventoDanoRecibido>(HandleDano);                // dañoInfligido/recibido
EventBus.Suscribir<EventoCuracion>(HandleCuracion);                // curacionTotal
EventBus.Suscribir<EventoMisionCompletada>(HandleMision);          // misionesCompletadas
EventBus.Suscribir<EventoTraitObtenido>(HandleTraitObtenido);      // traitStacks + globalState
EventBus.Suscribir<EventoEvolucionAplicada>(HandleEvolucion);
```

---

## Estado por Personaje y Estado Global

### EvolutionState (per-personaje, serializable)

```csharp
string characterId;
int nivelJugador;
float karma;
Dictionary<string, int> killsPorTipo;
Dictionary<string, int> usosHabilidad;
HashSet<string> habilidadesDesbloqueadas;
HashSet<string> misionesCompletadas;
int dañoInfligidoTotal, dañoRecibidoTotal, curacionTotal;
Dictionary<string, int> traitStacks;
HashSet<string> evolucionesAplicadas;
Dictionary<string, int> customFlags;
// ... más campos según se necesiten
```

Cada personaje del jugador tiene su **propio** EvolutionState. Cuando un evento ocurre (ej: kill), el EvolutionController actualiza el estado del personaje correcto usando `characterId`.

### GlobalPlayerState (compartido entre todos los personajes)

```csharp
HashSet<string> misionesGlobalesCompletadas;
HashSet<string> misionesGlobalesFallidas;
Dictionary<string, string> traitsGlobalmenteBloqueados;   // traitId → characterId que lo desbloqueó
Dictionary<string, string> misionesExclusivasAsignadas;   // misionId → characterId
Dictionary<string, int> flagsGlobales;
```

Al evaluar un `TraitDefinition` con `globalmenteUnico = true`, `EvolutionEvaluator` consulta `GlobalPlayerState` para verificar que ningún otro personaje ya lo tiene.

---

## Notas de Implementación
- Usa `seed` en `EvolutionState` para ofertas reproducibles
- Las exclusiones previenen combinaciones incoherentes
- Traits stackeables con `maxStacks` para efectos acumulativos
- `customFlags` para condiciones únicas sin tocar el enum (ej: `"union_iglesia"`)
- El campo `Fe` ya existe en `TipoRecurso`; el hook `OverridearRecursoPrincipal()` en `PaladinModulo` retorna null por ahora
- Todas las condiciones reciben `EvolutionState` — nunca referencias globales directas
