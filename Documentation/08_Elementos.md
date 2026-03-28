# Sistema Elemental

> Documentación del sistema de elementos: enum `ElementAttribute`, `ElementDefinition` (ScriptableObject de progresión), `ElementStatus` (estado de nivel por entidad) y `EntityStats` (componente Unity).

## Índice

- [Archivos Asociados](#archivos-asociados)
- [Visión General](#visión-general)
- [ElementAttribute (Enum)](#elementattribute-enum)
- [ElementDefinition (ScriptableObject)](#elementdefinition-scriptableobject)
- [ElementStatus](#elementstatus)
- [EntityStats (MonoBehaviour)](#entitystats-monobehaviour)
- [Flujo de Aplicación de Elemento](#flujo-de-aplicación-de-elemento)
- [Crear y Configurar Elementos en Unity](#crear-y-configurar-elementos-en-unity)
- [⚠ TODOs en código](#-todos-en-código)

---

## Archivos Asociados

| Archivo | Descripción |
|---------|-------------|
| [Assets/Scripts/Flags/Tipo.cs](../Assets/Scripts/Flags/Tipo.cs) | Define el enum `ElementAttribute` |
| [Assets/Scripts/SO/ElementDefinition.cs](../Assets/Scripts/SO/ElementDefinition.cs) | ScriptableObject de configuración y progresión de un elemento |
| [Assets/Scripts/Estados/ElementStatus.cs](../Assets/Scripts/Estados/ElementStatus.cs) | Estado de nivel/XP de un elemento en una entidad |
| [Assets/Scripts/Controllers/EntityStats.cs](../Assets/Scripts/Controllers/EntityStats.cs) | Componente Unity que aplica bonos elementales a la Entidad |

---

## Visión General

El sistema elemental **no es un triángulo de ventajas/desventajas**. Es un sistema de **progresión por elementos**: cada entidad puede poseer uno o más elementos, y cada elemento tiene un nivel que sube con XP y otorga bonos acumulativos de stats.

```
EntityStats (MonoBehaviour)
    ├── activeAttributes (ElementAttribute flags)
    └── activeStatuses (List<ElementStatus>)
            └── ElementStatus
                    ├── definition (ElementDefinition SO)
                    ├── level (1..maxLevel)
                    └── currentXP

ElementDefinition (ScriptableObject)
    ├── elementFlag (ElementAttribute)
    ├── baseDamageMultiplier, baseHealthBonus, baseDefenseBonus, baseSpeedBonus
    └── Progresión: xpPerLevel, xpScaling, maxLevel, damagePerLevel, healthPerLevel, ...
```

---

## ElementAttribute (Enum)

**Archivo**: `Assets/Scripts/Flags/Tipo.cs`

```csharp
[Flags]
public enum ElementAttribute
{
    None        = 0,
    Fire        = 1 << 0,
    Water       = 1 << 1,
    Light       = 1 << 2,
    Dark        = 1 << 3,
    Air         = 1 << 4,
    Geo         = 1 << 5,
    Electric    = 1 << 6,
    BloodSpilet = 1 << 7
}
```

> Es un `[Flags]` enum — una entidad puede tener múltiples elementos simultáneamente: `ElementAttribute.Fire | ElementAttribute.Dark`.

---

## ElementDefinition (ScriptableObject)

**Archivo**: `Assets/Scripts/SO/ElementDefinition.cs`  
**Menú Unity**: `Combate/Element Definition`  
**Ubicación sugerida**: `Assets/Resources/Elements/`

Define la configuración base y la progresión de un elemento.

### Propiedades

#### Identificación

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `elementName` | string | Nombre para mostrar |
| `elementFlag` | ElementAttribute | Flag del elemento |

#### Modificadores Base (Nivel 1)

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `baseDamageMultiplier` | float | Multiplicador de daño base (1.0 = sin cambio) |
| `baseHealthBonus` | int | Bonus de vida máxima (valor absoluto) |
| `baseDefenseBonus` | float | Bonus de defensa (valor absoluto) |
| `baseSpeedBonus` | int | Bonus de velocidad (valor absoluto) |

#### Progresión por Nivel

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `xpPerLevel` | float | XP base para subir del nivel 1 al 2 |
| `xpScaling` | float | Multiplicador de XP por nivel (`xpPerLevel * xpScaling^(nivel-2)`) |
| `maxLevel` | int | Nivel máximo del elemento |
| `damagePerLevel` | float | Incremento del multiplicador de daño por nivel |
| `healthPerLevel` | int | Incremento de HP máximo por nivel |
| `defensePerLevel` | float | Incremento de defensa por nivel |
| `speedPerLevel` | int | Incremento de velocidad por nivel |

#### Visual (Fase 3)

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `elementColor` | Color | Color representativo |
| `particlePrefab` | GameObject | Prefab de partículas |
| `activationSound` | AudioClip | Sonido al activar |

### XP Requerida por Nivel

```csharp
public float GetXPRequiredForLevel(int level)
{
    if (level <= 1) return 0;
    if (level > maxLevel) return float.MaxValue;
    return xpPerLevel * Mathf.Pow(xpScaling, level - 2);
}
```

### Ejemplo de Configuración: Fire

```
elementName: "Fuego"
elementFlag: Fire
baseDamageMultiplier: 1.1    ← +10% daño desde nivel 1
baseHealthBonus: 0
baseDefenseBonus: 0
baseSpeedBonus: 0

xpPerLevel: 1000
xpScaling: 1.45
maxLevel: 10
damagePerLevel: 0.1          ← +10% daño por nivel adicional
healthPerLevel: 50
defensePerLevel: 2
speedPerLevel: 5
```

---

## ElementStatus

**Archivo**: `Assets/Scripts/Estados/ElementStatus.cs`

Seguimiento del nivel y XP de un elemento específico en una entidad.

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `definition` | ElementDefinition | SO del elemento |
| `level` | int | Nivel actual (1..maxLevel) |
| `currentXP` | float | XP acumulada en el nivel actual |

### Métodos

```csharp
// Multiplicador de daño final (base + damagePerLevel * (level-1))
float GetFinalDamageMultiplier()

// Bonus de HP final (base + healthPerLevel * (level-1))
int GetFinalHealthBonus()

// Bonus de defensa final
float GetFinalDefenseBonus()

// Bonus de velocidad final
int GetFinalSpeedBonus()

// Añade XP; retorna true si subió de nivel
bool GainXP(float amount)

// Progreso de XP en el nivel actual (0..1)
float GetXPProgress()
```

---

## EntityStats (MonoBehaviour)

**Archivo**: `Assets/Scripts/Controllers/EntityStats.cs`

Componente Unity adjunto al GameObject de la entidad. Gestiona los elementos activos y aplica sus bonos a la `Entidad` lógica vinculada.

### Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `activeAttributes` | ElementAttribute | Flags de elementos activos |
| `activeStatuses` | List\<ElementStatus\> | Estado de nivel por cada elemento |
| `CurrentDamage` | int | Daño con bonos elementales aplicados |
| `CurrentMaxHealth` | int | HP máximo con bonos |
| `CurrentDefense` | float | Defensa con bonos |
| `CurrentSpeed` | int | Velocidad con bonos |

### Métodos Principales

```csharp
// Vincular con la Entidad lógica (llamar desde EntityController.Inicializar)
void VincularEntidad(Entidad entidad)

// Aplicar un elemento nuevo (busca definición en GameConfig)
void AplicarElemento(ElementAttribute elementFlag)

// Remover un elemento
void RemoverElemento(ElementAttribute elementFlag)

// Verificar si tiene un elemento
bool TieneElemento(ElementAttribute elemento)

// Añadir XP a un elemento activo
void AñadirXPAElemento(ElementAttribute elementFlag, float xpAmount)

// Re-sincronizar stats base desde Entidad y recalcular bonos
// (usar cuando las stats base cambian, ej: subida de nivel)
void ActualizarBaseYRecalcular()
```

### Flujo de Cálculo de Stats

`ApplyElementalModifiers()` calcula stats en tres pasos:

1. Reset a `baseDamage / baseHealth / baseDefense / baseSpeed` (sincronizados al vincular)
2. Acumula multiplicadores de daño y bonos aditivos de cada `ElementStatus` activo
3. Llama `AplicarStatsAEntidad()` → escribe en la `Entidad` lógica vía `AplicarBonusElementales()`

> **Importante**: `SincronizarStatsBase()` NO se llama dentro de `ApplyElementalModifiers()` para evitar que los bonos escritos en la Entidad se lean como "base" en la siguiente llamada (compounding).

---

## Flujo de Aplicación de Elemento

```
EntityStats.AplicarElemento(Fire)
        │
        ▼
GameConfig.GetDefinition(Fire)  ← busca el ElementDefinition SO
        │
        ├── ¿Ya tiene Fire?
        │       ├── Sí → ElementStatus.GainXP(50) → ¿subió de nivel?
        │       └── No → Crear ElementStatus nuevo, activeAttributes |= Fire
        │
        ▼
ApplyElementalModifiers()
        │
        ├── Resetear a stats base
        ├── Acumular multiplicadores/bonos de cada ElementStatus
        └── AplicarStatsAEntidad() → Entidad.AplicarBonusElementales(...)
```

---

## Crear y Configurar Elementos en Unity

1. **Assets → Create → Combate → Element Definition**
2. Configurar `elementName`, `elementFlag`, modificadores base y progresión
3. Abrir `Assets/Resources/GameConfig.asset` y agregar el mapping `ElementAttribute → ElementDefinition` en **Element Mappings**
4. Desde código, llamar `entityStats.AplicarElemento(ElementAttribute.Fire)` para activarlo

---

## ⚠ TODOs en código

| Archivo | TODO |
|---------|------|
| `EntityStats.cs` | La propiedad visual (color, partículas, sonido) de `ElementDefinition` está marcada como "Fase 3" — no está integrada aún. |
| `EntityStats.cs` | `GameConfig` se carga lazy en `Awake`; si el singleton no está disponible al primer uso, `AplicarElemento` logea un error y no aplica. |

---
