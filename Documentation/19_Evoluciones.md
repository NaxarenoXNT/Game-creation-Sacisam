# Sistema de Evoluciones y Traits

Sistema data-driven para evoluciones de clase y traits globales.

## Filosofía del Sistema

**Traits desbloquean Evoluciones:**
- Los **TRAITS** se desbloquean cumpliendo **condiciones genéricas** (kills, karma, misiones, etc.)
- Las **EVOLUCIONES** se desbloquean teniendo los **traits requeridos**
- Esto permite que los traits actúen como "logros" que habilitan opciones de evolución
- Permite crear evoluciones complejas sin hardcodear condiciones específicas

## Objetivos
- Evoluciones exclusivas por clase base; traits aplicables a casi todas las clases.
- Desbloqueo modular: traits por condiciones → evoluciones por traits
- Sistema extensible: agregar nuevas condiciones sin modificar código
- Ofertas ponderadas para reducir runs repetitivas

## ScriptableObjects

### EvolutionCondition (condición genérica)
Usada por **TraitDefinition** para determinar requisitos de desbloqueo.

```csharp
ConditionType tipo;    // Enum extensible con todos los tipos
string parametro;      // ID de enemigo, misión, item, facción, etc.
int cantidad;          // Valor entero requerido
float floatValor;      // Para karma/reputación (mínimo)
float floatValorMax;   // Para rangos (máximo)
string descripcionUI;  // Texto a mostrar en UI
```

**Tipos de Condiciones disponibles:**
| Categoría | Tipos |
|-----------|-------|
| Combate | `KillsTipo`, `KillsTotal`, `DañoInfligidoTotal`, `DañoRecibidoTotal`, `CuracionTotal`, `CombatesSinRecibirDaño`, `ComboMaximo` |
| Progresión | `NivelMinimo`, `TiempoJugado` |
| Karma | `KarmaMinimo`, `KarmaMaximo`, `KarmaRango` |
| Facciones | `ReputacionFaccion`, `RangoFaccion` |
| Misiones | `MisionCompletada`, `MisionesCompletadasTotal` |
| Items | `PoseeItem`, `ItemUsado`, `OroGastado`, `OroActual` |
| Habilidades | `HabilidadUsada`, `PoseeHabilidad` |
| Estados | `EstadoActivo`, `EstadoAplicadoVeces` |
| Exploración | `BiomaVisitado`, `BiomasVisitadosTotal` |
| Tags | `TieneTag` |
| Traits | `TieneTrait`, `TraitsTotal` |
| Especiales | `Sacrificios`, `MuertesJugador`, `EvolucionPrevia`, `Custom` |

### TraitDefinition (cross-class)
Se desbloquea cumpliendo **condiciones genéricas**.

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

// Desbloqueo
List<EvolutionCondition> condiciones;  // TODAS deben cumplirse
List<TraitDefinition> exclusiones;     // Traits mutuamente excluyentes

// Efectos
List<EvolutionEffect> efectos;
```

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

// Efectos
List<EvolutionEffect> efectos;
```

### EvolutionEffect (efecto atómico)
```csharp
EvolutionEffectType tipo;  // AddStatFlat, AddStatPercent, AddAbility, etc.
string stat;               // HP, ATK, DEF, VEL, MANA, MANA_MAX
float valor;
HabilidadData habilidad;   // Para AddAbility
string parametroExtra;     // Para efectos complejos
```

**Tipos de Efectos:**
- `AddStatFlat` / `AddStatPercent`
- `AddAbility` (HabilidadData ref)
- `ModifyCooldowns`
- `AddElement`
- `AddStatusPassive`
- `KarmaDelta` / `ReputationDelta`
- `WorldRuleToggle`
- `AITargetBias`
- `LootTableBias`
- `TagAdd`

### EvolutionBranch (árbol por clase)
```csharp
ClaseData claseOrigen;                     // Referencia al SO de clase
List<ClassEvolutionDefinition> evoluciones;
List<TraitDefinition> traitsRelacionados;  // Traits temáticos
```

## Componentes del Sistema

### EvolutionState
Estado runtime completo para evaluación:
- Contadores: kills, daño, curación, usos de items/habilidades
- Progresión: nivel, karma, reputaciones, rangos de facción
- Exploración: biomas visitados, tiempo jugado
- Traits y evoluciones obtenidas
- Custom flags para condiciones especiales

### EvolutionEvaluator
Evalúa disponibilidad:
1. **Traits**: Cumple TODAS las condiciones genéricas + clase no bloqueada + no excluido
2. **Evoluciones**: Tiene TODOS los traits requeridos + clase correcta + nivel mínimo

### EvolutionRoller
Genera ofertas ponderadas (2-3 opciones) usando `pesoOferta`.

### EvolutionApplier
Aplica `EvolutionEffect` al jugador (stats, habilidades, karma, etc.).

### EvolutionController (MonoBehaviour)
- Se suscribe al EventBus
- Actualiza EvolutionState
- Genera ofertas
- Expone API para UI
- Integra con SaveSystem

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
9. EvolutionApplier aplica efectos
           ↓
10. SaveSystem persiste estado
```

## Ejemplos de Diseño

### Ejemplo: Ruta del Paladín

**Trait: Cazador de No-Muertos**
```yaml
condiciones:
  - tipo: KillsTipo
    parametro: "NoMuerto"
    cantidad: 100
efectos:
  - tipo: TagAdd, valor: "cazador_nomuertos"
```

**Trait: Devoto de la Luz**
```yaml
condiciones:
  - tipo: KarmaMinimo
    floatValor: 0.3
  - tipo: ReputacionFaccion
    parametro: "Iglesia"
    floatValor: 50
efectos:
  - tipo: TagAdd, valor: "devoto"
```

**Evolución: Paladín**
```yaml
claseOrigen: Guerrero
claseDestino: Paladín
traitsRequeridos:
  - Cazador de No-Muertos
  - Devoto de la Luz
nivelMin: 5
efectos:
  - tipo: AddStatPercent, stat: HP, valor: 15
  - tipo: AddStatPercent, stat: DEF, valor: 15
  - tipo: AddAbility, habilidad: LuzSagrada
```

### Ejemplo: Ruta del Heraldo Maldito (Caída)

**Trait: Pacto Oscuro**
```yaml
condiciones:
  - tipo: TieneTag
    parametro: "pacto_demonio"
efectos:
  - tipo: AddElement, parametro: "Dark"
```

**Trait: Traidor de la Fe**
```yaml
condiciones:
  - tipo: TieneTrait
    parametro: "devoto"  # Requiere haber sido devoto
  - tipo: KarmaMaximo
    floatValor: -0.4
efectos:
  - tipo: ReputationDelta, parametro: "Iglesia", valor: -50
```

**Evolución: Heraldo Maldito**
```yaml
claseOrigen: Paladín  # ¡Requiere ya ser Paladín!
claseDestino: HeraldoMaldito
traitsRequeridos:
  - Pacto Oscuro
  - Traidor de la Fe
efectos:
  - tipo: AddStatPercent, stat: ATK, valor: 20
  - tipo: AddAbility, habilidad: LlamaProfana
  - tipo: WorldRuleToggle, parametro: "IglesiaHostil"
```

## Cómo agregar contenido sin código

### Nuevo Trait
1. `Create > Evolutions > Trait`
2. Definir id, nombre, descripción, icono
3. Añadir condiciones (lista de EvolutionCondition)
4. Añadir efectos

### Nueva Evolución
1. `Create > Evolutions > ClassEvolution`
2. Asignar claseOrigen y claseDestino (referencias a ClaseData)
3. Añadir traitsRequeridos (arrastra los TraitDefinition)
4. Añadir efectos

### Nueva Condición (requiere código)
1. Añadir entrada al `enum ConditionType`
2. Añadir case en `EvolutionEvaluator.EvaluarCondicion()`
3. Añadir campo correspondiente en `EvolutionState` si es necesario
4. Conectar evento en `EvolutionController`

## Hooks al EventBus
Conectar estos eventos para actualizar `EvolutionState`:

```csharp
// En tu EventBus
public static event Action<TipoEntidades> OnEnemigoDerrotado;
public static event Action<string> OnMisionCompletada;
public static event Action<string> OnHabilidadUsada;
public static event Action<float> OnKarmaModificado;
public static event Action<string, float> OnReputacionModificada;
public static event Action<string> OnBiomaEntrado;
public static event Action<int> OnNivelSubido;
public static event Action<string> OnEstadoAplicado;
public static event Action<int, int> OnDañoRegistrado; // infligido, recibido
```

## Notas de Implementación
- Usa `seed` en EvolutionState para ofertas reproducibles
- Las exclusiones previenen combinaciones incoherentes
- Traits stackeables con `maxStacks` para efectos acumulativos
- `customFlags` para condiciones únicas sin tocar el enum
