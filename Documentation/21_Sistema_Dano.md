# Sistema de Cálculo de Daño

## Visión General

El sistema de daño utiliza una fórmula centralizada que considera ataque físico, elemental, críticos, defensa y resistencias.

```
┌─────────────────────────────────────────────────────────────────┐
│                    FÓRMULA DE DAÑO                              │
│                                                                 │
│  BASE_OFFENSE = (ATK + ELEM_ATK) * RACE_ATK                    │
│  OFFENSE = BASE_OFFENSE * (isCrit ? CRIT_MULT : 1)             │
│  DEF_MULT = 1 / (1 + ln(1 + DEF * RACE_DEF) / K)               │
│  PHYSICAL_DAMAGE = OFFENSE * DEF_MULT                          │
│  ELEM_MULT = clamp(1 - RES_e, 0.1, 1.5)                        │
│  ELEMENTAL_DAMAGE = ELEM_ATK * ELEM_MULT                       │
│  FINAL_DAMAGE = PHYSICAL_DAMAGE + ELEMENTAL_DAMAGE             │
└─────────────────────────────────────────────────────────────────┘
```

---

## Archivos Principales

| Archivo | Descripción |
|---------|-------------|
| `Assets/Scripts/Combate/DamageCalculator.cs` | Calculadora central de daño |
| `Assets/Scripts/Combate/CombatStats.cs` | Estadísticas de combate (crit, elemental, resistencias) |
| `Assets/Scripts/Combate/CombatConfig.cs` | Configuración global (ScriptableObject) |
| `Assets/Scripts/Combate/RaceModifiers.cs` | Modificadores por tipo de entidad |

---

## Variables de la Fórmula

| Variable | Fuente | Descripción |
|----------|--------|-------------|
| `ATK` | `PuntosDeAtaque_Entidad` | Ataque físico base |
| `ELEM_ATK` | `CombatStats.elementalAttack` | Daño elemental adicional |
| `RACE_ATK` | `RaceModifiers` | Multiplicador por raza del atacante |
| `CRIT_CHANCE` | `CombatStats.critChance` | Probabilidad de crítico (0.0 - 1.0) |
| `CRIT_MULT` | `CombatStats.critMultiplier` | Multiplicador de daño crítico |
| `DEF` | `PuntosDeDefensa_Entidad` | Defensa del objetivo |
| `RACE_DEF` | `RaceModifiers` | Multiplicador de defensa por raza |
| `K` | `CombatConfig.defenseConstantK` | Constante de la fórmula (default: 5) |
| `RES_e` | `CombatStats.resistencias` | Resistencia elemental del objetivo |

---

## Curva de Defensa

La fórmula `DEF_MULT = 1 / (1 + ln(1 + DEF) / K)` con K=5 produce:

| Defensa | % Daño Recibido | % Mitigación |
|---------|-----------------|--------------|
| 100 | ~52% | ~48% |
| 500 | ~42% | ~58% |
| 1,000 | ~38% | ~62% |
| 5,000 | ~30% | ~70% |
| 10,000 | ~27% | ~73% |
| 50,000 | ~21% | ~79% |

La curva logarítmica asegura que la defensa siempre sea útil pero nunca llegue a 100% de mitigación.

---

## CombatStats

```csharp
public class CombatStats
{
    // Críticos
    public float critChance = 0.05f;        // 5% base
    public float critMultiplier = 1.5f;     // x1.5 daño
    
    // Elemental
    public int elementalAttack = 0;         // Daño elemental extra
    public ElementAttribute elementoAtaque; // Tipo de elemento
    
    // Resistencias
    public ElementalResistances resistencias;
    
    // Si el crítico aplica también al daño elemental
    public bool critAppliesToElemental = false;
}
```

### Resistencias Elementales

```csharp
public class ElementalResistances
{
    public float fire = 0f;      // -0.5 a 1.0
    public float water = 0f;
    public float light = 0f;
    public float dark = 0f;
    public float air = 0f;
    public float geo = 0f;
    public float electric = 0f;
    public float bloodSpilet = 0f;
}
```

| Valor | Efecto |
|-------|--------|
| -0.5 | Vulnerable (+50% daño elemental) |
| 0 | Neutro (daño normal) |
| 0.5 | Resistente (-50% daño elemental) |
| 1.0 | Inmune (mínimo 10% daño) |

---

## Uso Básico

### Desde una Entidad

```csharp
// El método CalcularDanoContra ya usa la fórmula completa
int dano = atacante.CalcularDanoContra(defensor);

// Para obtener detalles (si fue crítico, daño elemental, etc)
DamageResult resultado = atacante.CalcularDanoContraConResultado(defensor);

if (resultado.isCritical)
    Debug.Log("¡CRÍTICO!");

Debug.Log($"Físico: {resultado.physicalDamage}, Elemental: {resultado.elementalDamage}");
```

### Cálculo Manual

```csharp
using Combate;

// Preparar datos
var attackerData = new AttackerData
{
    attack = 500,
    elementalAttack = 100,
    attackElement = ElementAttribute.Fire,
    critChance = 0.2f,
    critMultiplier = 2.0f,
    entityType = TipoEntidades.Humanoid
};

var defenderData = new DefenderData
{
    defense = 1000,
    resistances = new ElementalResistances { fire = 0.3f },
    entityType = TipoEntidades.Beast
};

DamageResult result = DamageCalculator.CalculateDamage(attackerData, defenderData);
```

---

## RaceModifiers (ScriptableObject)

Configura modificadores de daño entre tipos de entidad.

### Crear Asset

1. **Project → Create → Combate → Race Modifiers**
2. Configurar modificadores

### Ejemplo de Configuración

```yaml
Attack Modifiers:
  - Humanoid: 1.0x (normal)
  - Beast: 1.1x (+10% daño)
  - Undead: 0.9x (-10% daño)
  - Elemental: 1.2x (+20% daño)

Defense Modifiers:
  - Humanoid: 1.0x (defensa normal)
  - Beast: 0.8x (defensa menos efectiva)
  - Undead: 1.2x (defensa más efectiva)
  - Elemental: 0.9x

Race vs Race:
  - Humanoid → Undead: 0.8x (menos efectivo)
  - Light → Dark: 1.5x (muy efectivo)
```

---

## CombatConfig (ScriptableObject)

Configuración global del sistema de combate.

### Crear Asset

1. **Project → Create → Combate → Combat Config**
2. Guardar en `Assets/Resources/CombatConfig.asset`

### Propiedades

```csharp
[Header("Fórmula de Defensa")]
public float defenseConstantK = 5f;

[Header("Multiplicadores Elementales")]
public float minElementalMultiplier = 0.1f;  // Daño mínimo
public float maxElementalMultiplier = 1.5f;  // Daño máximo

[Header("Críticos")]
public float baseCritChance = 0.05f;      // 5% base
public float baseCritMultiplier = 1.5f;   // x1.5 base

[Header("Referencias")]
public RaceModifiers raceModifiers;       // Modificadores de raza

[Header("Debug")]
public bool debugDamageCalculation = false;
```

---

## Ejemplos de Entidades

### Dragon (30% crit, x2 daño)

```csharp
public Dragon(EnemigoData datos) : base(...)
{
    CombatStats.critChance = 0.30f;
    CombatStats.critMultiplier = 2.0f;
    CombatStats.elementoAtaque = ElementAttribute.Fire;
}
```

### Mago Elemental (crítico aplica a elemental)

```csharp
public MagoElemental(ClaseData datos) : base(...)
{
    CombatStats.critChance = 0.15f;
    CombatStats.critMultiplier = 1.8f;
    CombatStats.elementalAttack = 200;
    CombatStats.elementoAtaque = ElementAttribute.Fire;
    CombatStats.critAppliesToElemental = true;  // ¡Crítico elemental!
}
```

### Tanque con Resistencias

```csharp
public Paladin(ClaseData datos) : base(...)
{
    CombatStats.critChance = 0.05f;
    CombatStats.resistencias.fire = 0.3f;    // 30% resistencia fuego
    CombatStats.resistencias.dark = -0.2f;   // 20% vulnerable oscuridad
}
```

---

## DamageResult

Estructura con el resultado detallado del cálculo.

```csharp
public struct DamageResult
{
    public int finalDamage;         // Daño total final
    public int physicalDamage;      // Componente físico
    public int elementalDamage;     // Componente elemental
    public bool isCritical;         // ¿Fue crítico?
    public float defenseMultiplier; // Multiplicador de defensa aplicado
    public float elementalMultiplier; // Multiplicador elemental aplicado
    public float raceAtkMultiplier; // Multiplicador de raza (ataque)
    public float raceDefMultiplier; // Multiplicador de raza (defensa)
}
```

### Uso en UI

```csharp
DamageResult resultado = atacante.CalcularDanoContraConResultado(objetivo);

// Mostrar número de daño
string texto = resultado.finalDamage.ToString();
Color color = resultado.isCritical ? Color.yellow : Color.white;

// Efectos adicionales
if (resultado.isCritical)
    PlayCriticalEffect();
    
if (resultado.elementalDamage > resultado.physicalDamage)
    PlayElementalEffect(atacante.CombatStats.elementoAtaque);
```

---

## Ajustar K para tu Juego

La constante K controla qué tan efectiva es la defensa:

| K | DEF=1000 Mitiga | DEF=10000 Mitiga | Descripción |
|---|-----------------|------------------|-------------|
| 3 | 70% | 79% | Defensa muy fuerte |
| 5 | 62% | 73% | **Recomendado** |
| 7 | 55% | 68% | Defensa moderada |
| 10 | 48% | 62% | Defensa débil |

Para ajustar: modifica `CombatConfig.defenseConstantK` en el asset.
