# Sistema de Evoluciones, Traits y Chains

## Índice
1. [Arquitectura General](#arquitectura-general)
2. [Condiciones (EvolutionConditionSO)](#condiciones-evolutionconditionso)
3. [Traits Individuales](#traits-individuales)
4. [Cadenas de Traits (TraitChain)](#cadenas-de-traits-traitchain)
5. [Flujo de Creación Completo](#flujo-de-creación-completo)
6. [Ejemplos Prácticos](#ejemplos-prácticos)

---

## Arquitectura General

```
┌─────────────────────────────────────────────────────────────────┐
│                    SISTEMA DE EVOLUCIONES                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────┐     ┌──────────────────┐                  │
│  │ EvolutionConditionSO │◄────│  TraitDefinition  │                  │
│  │   (Condiciones)      │     │  (Traits simples) │                  │
│  └──────────────────┘     └──────────────────┘                  │
│           │                          │                           │
│           │                          │                           │
│           ▼                          ▼                           │
│  ┌──────────────────┐     ┌──────────────────┐                  │
│  │ TraitChainDefinition │────►│ ClassEvolutionDef │                  │
│  │ (Cadenas de traits)  │     │ (Evolución final) │                  │
│  └──────────────────┘     └──────────────────┘                  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Principios del Sistema

- **Condiciones como ScriptableObjects**: Cada condición es un SO independiente y reutilizable
- **Separación de responsabilidades**: Condiciones, Traits y Chains son entidades separadas
- **Escalabilidad**: Las condiciones pueden escalarse automáticamente en cadenas
- **Sin problemas de serialización**: Unity serializa correctamente cada SO concreto

---

## Condiciones (EvolutionConditionSO)

### Estructura Base

```
Assets/Scripts/Evolution/Conditions/
├── EvolutionConditionSO.cs      ← Clase abstracta base
├── KillsConditionSO.cs          ← Mata X de tipo Y
├── KillsTotalConditionSO.cs     ← Mata X total
├── KarmaConditionSO.cs          ← Karma mín/máx/rango
├── TraitConditionSO.cs          ← Requiere trait
├── NivelConditionSO.cs          ← Nivel mínimo
├── SacrificiosConditionSO.cs    ← X sacrificios
├── MisionConditionSO.cs         ← Misión completada
├── EstadoConditionSO.cs         ← Estado de combate
├── DanoInfligidoConditionSO.cs  ← Daño total
├── CuracionConditionSO.cs       ← Curación total
└── CustomConditionSO.cs         ← Flags personalizados
```

### Crear una Condición

1. **Click derecho** en la carpeta donde quieras guardarla
2. **Create > Evolutions > Conditions > [Tipo]**
3. **Configurar** los parámetros específicos

### Tipos de Condiciones Disponibles

| Tipo | Menú | Campos | Ejemplo |
|------|------|--------|---------|
| **Kills Tipo** | Conditions/Kills Tipo | `tipoEntidad`, `cantidad` | Mata 50 Undead |
| **Kills Total** | Conditions/Kills Total | `cantidad` | Mata 100 enemigos |
| **Karma** | Conditions/Karma | `comparacion`, `valor`, `valorMax` | Karma ≥ 0.5 |
| **Trait** | Conditions/Tiene Trait | `traitRequerido` o `traitId` | Requiere "Sacrificios II" |
| **Nivel** | Conditions/Nivel Minimo | `nivelMinimo` | Nivel 20+ |
| **Sacrificios** | Conditions/Sacrificios | `cantidad` | 10 sacrificios |
| **Misión** | Conditions/Mision Completada | `misionId` | Completa "ritual_oscuro" |
| **Estado** | Conditions/Estado Aplicado | `estado`, `vecesAplicado` | Aplica Quemado 50 veces |
| **Daño** | Conditions/Daño Infligido | `cantidad` | Inflige 10000 daño |
| **Curación** | Conditions/Curación Total | `cantidad` | Cura 5000 HP |
| **Custom** | Conditions/Custom Flag | `flagKey`, `valorMinimo` | Flag especial |

### Propiedades Comunes

Todas las condiciones heredan de `EvolutionConditionSO` y se evalúan contra el `EvolutionState` de un **personaje específico**:

```csharp
// Campos compartidos
public string descripcionUI;  // Descripción manual (opcional)
public Sprite icono;          // Icono para UI

// Métodos que cada condición implementa
bool Evaluar(EvolutionState state);           // ¿Se cumple para este personaje?
float GetProgreso(EvolutionState state);      // 0.0 a 1.0
string GetDescripcionAuto();                  // Texto automático
EvolutionConditionSO CrearCopiaEscalada(float mult);  // Para chains
```

> **Multi-Personaje:** Cada condición recibe el `EvolutionState` del personaje evaluado — nunca un estado global. Esto permite que dos personajes tengan progreso independiente en la misma condición.

### Escalabilidad

Algunas condiciones son **escalables** (tienen `EsEscalable = true`):
- KillsConditionSO
- NivelConditionSO
- SacrificiosConditionSO
- etc.

Esto permite que en las cadenas, las cantidades se multipliquen automáticamente.

---

## Traits Individuales

### Crear un Trait

1. **Create > Evolutions > Trait**
2. Configurar:
   - `id`: Identificador único (ej: "vampirismo")
   - `nombreMostrar`: Nombre para UI
   - `descripcion`: Texto descriptivo
   - `condiciones`: **Arrastra los SOs de condición aquí**
   - `efectos`: Efectos al obtener el trait
   - `exclusiones`: Traits incompatibles

### Estructura del TraitDefinition

```
TraitDefinition
├── Identidad
│   ├── id (string único)
│   ├── nombreMostrar
│   ├── descripcion
│   ├── icono
│   └── rareza
├── Restricciones
│   ├── clasesBloqueadas
│   ├── stackeable
│   └── maxStacks
├── Condiciones de Desbloqueo
│   └── List<EvolutionConditionSO>  ← Referencias a SOs
├── Exclusiones
│   └── List<TraitDefinition>
└── Efectos
    └── List<EvolutionEffect>
```

### Ejemplo: Trait "Vampirismo"

```
📁 Traits/
   └── Trait_Vampirismo.asset
       ├── id: "vampirismo"
       ├── nombreMostrar: "Vampirismo"
       ├── condiciones:
       │   ├── Cond_Kills_Undead_30.asset
       │   └── Cond_Karma_Negativo.asset
       └── efectos: [+10% Lifesteal]
```

---

## Cadenas de Traits (TraitChain)

Las cadenas permiten definir **progresiones lineales** de traits donde cada nivel desbloquea el siguiente.

### Crear una Cadena

1. **Create > Evolutions > Trait Chain**
2. Configurar la identidad de la cadena
3. Agregar **condiciones base** (para el nivel I)
4. Definir los **nodos** de progresión

### Estructura del TraitChainDefinition

```
TraitChainDefinition
├── Identidad
│   ├── idBase: "sacrificios"
│   ├── nombreBase: "Sacrificios"
│   ├── descripcionGeneral
│   ├── iconoBase
│   └── rarezaBase
├── Restricciones
│   ├── clasesBloqueadas
│   └── exclusionesGlobales
├── Condiciones Base (Nivel 1)
│   └── List<EvolutionConditionSO>
├── Nodos de Progresión
│   └── List<TraitChainNode>
│       ├── [0] Nodo I
│       ├── [1] Nodo II
│       └── [2] Nodo III
└── Evolución Final (Opcional)
    ├── evolucionFinal: ClassEvolutionDefinition
    └── condicionesEvolucionFinal
```

### TraitChainNode (Cada nivel)

```
TraitChainNode
├── sufijo: "I", "II", "III"...
├── descripcion: "Texto específico de este nivel"
├── condicionesAdicionales: List<EvolutionConditionSO>
├── efectos: List<EvolutionEffect>
├── multiplicadorCantidad: 1.5 (escala las condiciones base)
└── heredaCondicionesBase: true/false
```

### Cómo Funciona el Escalado

Si `heredaCondicionesBase = true`:

```
Condición Base: 10 sacrificios
Multiplicador Nodo I: 1.0  → 10 sacrificios
Multiplicador Nodo II: 1.5 → 15 sacrificios (10 × 1.5)
Multiplicador Nodo III: 1.5 → 22 sacrificios (10 × 1.5 × 1.5)
```

### IDs Generados Automáticamente

La cadena genera IDs concatenando `idBase` + `_` + `sufijo`:

```
idBase: "sacrificios"
Nodos: I, II, III

IDs generados:
├── sacrificios_i
├── sacrificios_ii
└── sacrificios_iii
```

---

## Flujo de Creación Completo

### Paso 1: Crear las Condiciones Reutilizables

```
📁 Assets/Resources/Conditions/
   ├── Cond_Sacrificios_10.asset    (SacrificiosConditionSO, cantidad=10)
   ├── Cond_Karma_Negativo.asset    (KarmaConditionSO, máximo=-0.3)
   ├── Cond_Nivel_15.asset          (NivelConditionSO, nivelMinimo=15)
   └── Cond_Kills_Undead_50.asset   (KillsConditionSO, tipo=Undead, cantidad=50)
```

### Paso 2: Crear Traits Individuales (si aplica)

```
📁 Assets/Resources/Traits/
   └── Trait_Vampirismo.asset
       └── condiciones: [Cond_Kills_Undead_50, Cond_Karma_Negativo]
```

### Paso 3: Crear Cadenas de Traits

```
📁 Assets/Resources/TraitChains/
   └── Chain_Sacrificios.asset
       ├── idBase: "sacrificios"
       ├── nombreBase: "Sacrificios"
       ├── condicionesBase: [Cond_Sacrificios_10]
       └── nodos:
           ├── [0] sufijo: "I",   multiplicador: 1.0
           ├── [1] sufijo: "II",  multiplicador: 1.5
           ├── [2] sufijo: "III", multiplicador: 1.5, condicionesAdicionales: [Cond_Nivel_15]
           └── [3] sufijo: "IV",  multiplicador: 2.0, condicionesAdicionales: [Cond_Karma_Negativo]
```

### Paso 4: Conectar con Evolución Final (opcional)

```
Chain_Sacrificios.asset
└── evolucionFinal: Evo_Emomancer.asset
```

---

## Ejemplos Prácticos

### Ejemplo 1: Cadena del Emomancer

**Objetivo**: Desbloquear la evolución "Emomancer" completando la cadena de sacrificios.

#### 1. Crear condiciones:

| Asset | Tipo | Configuración |
|-------|------|---------------|
| `Cond_Sacrificios_10.asset` | Sacrificios | cantidad: 10 |
| `Cond_Karma_Bajo.asset` | Karma | comparacion: Maximo, valor: -0.2 |
| `Cond_Nivel_20.asset` | Nivel | nivelMinimo: 20 |

#### 2. Crear la cadena:

```yaml
# Chain_Sacrificios.asset
idBase: sacrificios
nombreBase: Sacrificios
condicionesBase: 
  - Cond_Sacrificios_10.asset

nodos:
  - sufijo: "I"
    multiplicadorCantidad: 1.0
    heredaCondicionesBase: true
    efectos: [+5% Daño Oscuro]
    
  - sufijo: "II"
    multiplicadorCantidad: 1.5
    heredaCondicionesBase: true
    efectos: [+10% Daño Oscuro]
    
  - sufijo: "III"
    multiplicadorCantidad: 2.0
    heredaCondicionesBase: true
    condicionesAdicionales:
      - Cond_Karma_Bajo.asset
    efectos: [+15% Daño Oscuro, Lifesteal 5%]
    
  - sufijo: "IV"
    multiplicadorCantidad: 2.5
    heredaCondicionesBase: true
    condicionesAdicionales:
      - Cond_Nivel_20.asset
    efectos: [+20% Daño Oscuro, Lifesteal 10%]

evolucionFinal: Evo_Emomancer.asset
```

#### 3. Resultado en el juego:

| Nodo | Requisitos | ID Generado |
|------|------------|-------------|
| I | 10 sacrificios | sacrificios_i |
| II | 15 sacrificios + Sacrificios I | sacrificios_ii |
| III | 20 sacrificios + Karma ≤ -0.2 + Sacrificios II | sacrificios_iii |
| IV | 25 sacrificios + Nivel 20 + Sacrificios III | sacrificios_iv |
| **Evolución** | Completar toda la cadena | → Emomancer |

---

### Ejemplo 2: Trait Individual (Sin cadena)

**Objetivo**: Trait "Cazador de No-Muertos" que se obtiene matando undeads.

#### 1. Crear condición:

```yaml
# Cond_Kills_Undead_100.asset
tipo: KillsConditionSO
tipoEntidad: Undead
cantidad: 100
descripcionUI: "Elimina 100 no-muertos"
```

#### 2. Crear trait:

```yaml
# Trait_CazadorUndeads.asset
id: cazador_undeads
nombreMostrar: "Cazador de No-Muertos"
descripcion: "Has probado tu valía contra las hordas de no-muertos"
rareza: Rare
condiciones:
  - Cond_Kills_Undead_100.asset
efectos:
  - +25% daño contra Undead
  - +10% resistencia a Oscuro
```

---

## Ubicación de Archivos

```
Assets/
├── Resources/
│   ├── Conditions/          ← Condiciones reutilizables
│   │   ├── Kills/
│   │   ├── Karma/
│   │   └── Misc/
│   ├── Traits/              ← Traits individuales
│   └── TraitChains/         ← Cadenas de traits
├── Scripts/
│   └── Evolution/
│       ├── Conditions/      ← Scripts de condiciones
│       ├── TraitDefinition.cs
│       ├── TraitChainDefinition.cs
│       └── ...
└── Editor/
    └── TraitChainDefinitionEditor.cs
```

---

## Tips y Buenas Prácticas

1. **Nombra las condiciones descriptivamente**: `Cond_Kills_Beast_50`, `Cond_Karma_Positivo`

2. **Reutiliza condiciones**: La misma condición puede usarse en múltiples traits/chains

3. **Usa el Editor visual**: El editor de TraitChain muestra una vista previa de la cadena

4. **Valida antes de usar**: El botón "Validar Cadena" detecta problemas de configuración

5. **Organiza por carpetas**: Agrupa condiciones similares en subcarpetas

6. **Documenta en descripcionUI**: Ayuda a entender qué hace cada condición en el Inspector
