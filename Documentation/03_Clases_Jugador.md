# Clases del Jugador

## Visión General

Las clases del jugador heredan de `Jugador` (abstracta) y añaden mecánicas únicas de clase.
Además, cada clase puede recibir **módulos de evolución** (`IComportamientoDeClase`) en runtime
sin necesidad de cambiar su tipo de objeto.

```
Jugador (abstracta)
├── Guerrero      ← mecánica: +15% a toda ganancia de defensa
├── Mago          ← mecánica: distribución XP 60/40 (más XP a elementos)
└── Arquero       ← mecánica: crédito garantizado mientras está en sigilo
       +
   List<IComportamientoDeClase>   ← módulos de evolución (Paladín, etc.)
```

Archivos relevantes:

| Archivo | Descripción |
|---|---|
| `Assets/Scripts/Padres/Jugador.cs` | Clase base abstracta, sistema de módulos |
| `Assets/Scripts/Subclases/ClasesDelJugador/Guerrero.cs` | Clase Guerrero |
| `Assets/Scripts/Subclases/ClasesDelJugador/Mago.cs` | Clase Mago |
| `Assets/Scripts/Subclases/ClasesDelJugador/Arquero.cs` | Clase Arquero |
| `Assets/Scripts/Subclases/Modulos/` | Módulos de evolución de clase |

---

## Guerrero

**Namespace**: `Subclases`  
**Ruta**: `Assets/Scripts/Subclases/ClasesDelJugador/Guerrero.cs`

### Mecánica Única — Maestría Defensiva

Toda ganancia de defensa (por nivel **o** por fuente externa: traits, pasivas, evoluciones)
recibe un bonus adicional del **+15%**.

- **Subida de nivel**: `AplicarEscaladoNivel()` aplica el escalado base y luego suma
  `defensaPorNivel × 0.15f` encima.
- **Cualquier otra fuente**: `ModificarDefensa(float cantidad)` está override; si
  `cantidad > 0` la multiplica por `1.15f` antes de aplicarla. Las pérdidas de defensa
  no se ven afectadas.

```csharp
// Ejemplo: si defensaPorNivel = 2.0 y el jugador sube de nivel:
// Base: +2.0 DEF
// Bonus Guerrero: +0.3 DEF  (2.0 × 0.15)
// Total: +2.3 DEF

// Ejemplo: si un trait otorga +10 DEF:
// Base: +10.0 DEF
// Bonus Guerrero: +1.5 DEF  (10 × 0.15)
// Total: +11.5 DEF
```

**Constante configurable en código**: `BonusDefensaPorcentaje = 0.15f`

### Evolución disponible: Paladín

Ver sección de módulos más abajo y el documento `19_Evoluciones.md`.

---

## Mago

**Namespace**: `Subclases`  
**Ruta**: `Assets/Scripts/Subclases/ClasesDelJugador/Mago.cs`

### Mecánica Única — Sinergia Elemental

La distribución de XP cambia de la estándar (80/20) a **60% jugador / 40% elementos**.
El Mago sube de nivel más lento pero sus elementos progresan significativamente más rápido.

| | Estándar | Mago |
|---|---|---|
| XP al jugador | 80% | 60% |
| XP a elementos | 20% | 40% |

Implementado sobreescribiendo las propiedades virtuales de `Jugador`:

```csharp
protected override float PropXPJugador   => 0.6f;
protected override float PropXPElementos => 0.4f;
```

---

## Arquero

**Namespace**: `Subclases`  
**Ruta**: `Assets/Scripts/Subclases/ClasesDelJugador/Arquero.cs`

### Mecánica Única — Sigilo

Mientras el Arquero está en estado de sigilo, **cada ataque es un crítico garantizado**.

- Al atacar: si el objetivo **muere** del golpe, permanece en sigilo.
- Si el objetivo **sobrevive**, sale automáticamente del sigilo.

#### API pública

```csharp
Arquero arquero = ...;

// Activar sigilo (llamar desde habilidad, item, evento de mundo...)
arquero.EntrarEnSigilo();

// Consultar estado
bool estaOculto = arquero.EstaInvisible;

// Salir manualmente (normalmente ocurre automático al atacar sin matar)
arquero.SalirDeSigilo();
```

#### Flujo técnico

1. `ForzarCritico()` retorna `EstaInvisible` → `Entidad.CalcularDanoContraConResultado()`
   lee este valor y fuerza `critChance = 1f` antes de ejecutar el pipeline.
2. `PostAtaqueConContexto(ctx, objetivoMurio)` (override de `Jugador`) llama
   `SalirDeSigilo()` si el objetivo sobrevivió.

---

## Sistema de Módulos de Evolución

Las clases principales no se reemplazan al evolucionar; en su lugar se les **inyectan
módulos de comportamiento** (`IComportamientoDeClase`) que modifican sus mecánicas en
runtime sin cambiar el tipo de objeto.

### Reglas de consulta

| Tipo de hook | Iteración | Comportamiento |
|---|---|---|
| **Aditivo** (curación ±%) | 0 → Count | Todos los módulos contribuyen en cadena |
| **Sustitutivo** (elemento de ataque, recurso) | Count-1 → 0 | El módulo más reciente que responda gana |

### Hooks disponibles en `IComportamientoDeClase`

| Hook | Tipo | Descripción |
|---|---|---|
| `ModificarCuracionOtorgada` | Aditivo | Curación que el jugador da a otros |
| `ModificarCuracionRecibida` | Aditivo | Curación que el jugador recibe |
| `ModificarElementoAtaque` | Sustitutivo | Override del elemento de ataque |
| `OverridearRecursoPrincipal` | Sustitutivo | Cambiar Mana por Fe u otro recurso |

### API de módulos en `Jugador`

```csharp
// Agregar un módulo (evita duplicados por ModuloId)
jugador.AgregarModulo(modulo);

// Remover un módulo por ID
jugador.RemoverModulo("paladin");

// Leer la lista (read-only)
IReadOnlyList<IComportamientoDeClase> modulos = jugador.Modulos;
```

### Módulos implementados

| Módulo | ID | Evolución de |
|---|---|---|
| `PaladinModulo` | `"paladin"` | Guerrero |
| `HeraldoCaidoModulo` | `"heraldo_caido"` | Paladín (stub, pendiente completar) |

### Ejemplo: Guerrero → Paladín → Heraldo Caído

```
Estado del jugador (Guerrero):   [  ]
Después de evolucionar a Paladín: [ PaladinModulo ]
Después de Heraldo Caído:         [ PaladinModulo, HeraldoCaidoModulo ]

Consulta de elemento (sustitutivo, iteración reversa):
  HeraldoCaidoModulo.ModificarElementoAtaque(None) → Dark  ← gana
  PaladinModulo no se consulta.

Consulta de curación (aditivo, iteración normal):
  PaladinModulo.ModificarCuracionRecibida(100) → 120   (+20%)
  HeraldoCaidoModulo.ModificarCuracionRecibida(120) → 120  (sin cambio)
  Resultado: 120
```

### Crear un NPC ya evolucionado

En el `ClaseData` del NPC, aggrega los módulos en el campo `modulosIniciales`:

1. Crear SO del módulo: *Assets → Create → Clases/Modulos/Paladin*
2. Arrastrarlo a `ClaseData.modulosIniciales`
3. `ClaseData.CrearInstancia()` los aplicará automáticamente al instanciar

---

## Crear una Nueva Clase de Jugador

### Paso 1: Crear la subclase

```csharp
// Assets/Scripts/Subclases/ClasesDelJugador/NuevaClase.cs
using Padres;

namespace Subclases
{
    public class NuevaClase : Jugador
    {
        public NuevaClase(ClaseData datos) : base(
            datos.nombreClase, datos.vidaBase, datos.ataqueBase,
            datos.defensaBase, 1, datos.manaBase, datos.velocidadBase,
            datos.atributos, datos.tipoEntidad, datos.estiloCombate, datos.escalado)
        {
            InicializarDesdeClaseData(datos);
        }

        // --- Mecánica única (opciones) ---

        // Override de stats por nivel:
        protected override void AplicarEscaladoNivel() { /* ... */ }

        // Override de distribución de XP:
        protected override float PropXPJugador   => 0.7f;
        protected override float PropXPElementos => 0.3f;

        // Override de crítico garantizado:
        public override bool ForzarCritico() => /* condición */;

        // Override de curación recibida (si no usa módulos):
        protected override int ModificarCuracionRecibida(int cantidad) => /* ... */;
    }
}
```

### Paso 2: Registrar en `ClaseData.CrearInstancia()`

```csharp
"NuevaClase" => new NuevaClase(this),
```

### Paso 3: Crear el SO de ClaseData en Unity

*Assets → Create → Combate → Clase Jugador* → configurar nombre = `"NuevaClase"`

---

## Tabla Comparativa

| Clase | Mecánica Única | Escalado DEF/Nv | XP jugador |
|---|---|---|---|
| Guerrero | +15% a toda ganancia de DEF | escalado × 1.15 | 80% |
| Mago | XP 60/40 (más a elementos) | estándar | 60% |
| Arquero | Crítico garantizado en sigilo | estándar | 80% |


### Características

| Característica | Valor |
|---------------|-------|
| Estilo de Combate | Melee |
| Tipo de Entidad | Jugador |
| Fortaleza | Alta vida y defensa |
| Debilidad | Baja velocidad |

### Escalado Específico

```csharp
private static readonly EscaladoJugador EscaladoGuerrero = new EscaladoJugador
{
    vidaPorNivel = 15,      // +15 HP por nivel (más que promedio)
    ataquePorNivel = 4,     // +4 ATK por nivel
    defensaPorNivel = 3f,   // +3 DEF por nivel (más que promedio)
    manaPorNivel = 3,       // +3 MP por nivel (menos que promedio)
    velocidadPorNivel = 1   // +1 VEL por nivel
};
```

### Cálculo de Daño

```csharp
public override int CalcularDanoContra(IEntidadCombate objetivo)
{
    // Guerreros hacen daño estándar basado en ATK
    return PuntosDeAtaque_Entidad;
}
```

### Configuración en ClaseData

```
Nombre: "Guerrero"
Tipo Clase: Guerrero
Vida Base: 120
Ataque Base: 15
Defensa Base: 10
Mana Base: 30
Velocidad Base: 8
Atributos: None
Tipo Entidad: Jugador
Estilo Combate: Melee
```

---

## Crear una Nueva Clase de Jugador

### Paso 1: Crear la Subclase

```csharp
// Assets/Scripts/Subclases/Mago.cs
using Padres;
using Interfaces;

namespace Subclases
{
    public class Mago : Jugador
    {
        // Escalado específico del Mago
        private static readonly EscaladoJugador EscaladoMago = new EscaladoJugador
        {
            vidaPorNivel = 8,       // Menos HP
            ataquePorNivel = 2,     // Menos ATK físico
            defensaPorNivel = 1f,   // Menos defensa
            manaPorNivel = 10,      // Mucho más mana
            velocidadPorNivel = 2   // Más velocidad
        };

        public Mago(
            string nombre,
            int vidaBase,
            int ataqueBase,
            float defensaBase,
            int nivel,
            int manaBase,
            int velocidadBase,
            ElementAttribute atributos,
            TipoEntidades tipoEntidad,
            CombatStyle estiloCombate
        ) : base(
            nombre, vidaBase, ataqueBase, defensaBase,
            nivel, manaBase, velocidadBase,
            atributos, tipoEntidad, estiloCombate,
            EscaladoMago
        )
        {
        }

        public override int CalcularDanoContra(IEntidadCombate objetivo)
        {
            // Los magos hacen daño basado en nivel además de ATK
            return PuntosDeAtaque_Entidad + (Nivel_Entidad * 2);
        }
    }
}
```

### Paso 2: Añadir al Enum TipoClase

```csharp
// En el archivo correspondiente de enums
public enum TipoClase
{
    Guerrero,
    Mago,    // Añadir aquí
    Arquero  // Y más clases
}
```

### Paso 3: Actualizar ClaseData.CrearInstancia()

```csharp
public Jugador CrearInstancia()
{
    switch (tipoClase)
    {
        case TipoClase.Guerrero:
            return new Guerrero(this);
            
        case TipoClase.Mago:    // Añadir case
            return new Mago(
                nombre, vidaBase, ataqueBase, defensaBase,
                1, manaBase, velocidadBase,
                atributos, tipoEntidad, estiloCombate
            );
            
        default:
            return new Guerrero(this);
    }
}
```

### Paso 4: Crear ClaseData en Unity

1. Click derecho > Create > Combate > Clase Data
2. Configurar valores base
3. Seleccionar `TipoClase.Mago`

---

## Ideas para Otras Clases

### Arquero
```csharp
// Características:
// - Estilo: Ranged
// - Alta velocidad, ataque medio
// - Puede atacar primero
// - Bonus de daño contra objetivos con vida completa

public override int CalcularDanoContra(IEntidadCombate objetivo)
{
    int dano = PuntosDeAtaque_Entidad;
    
    // Bonus si el objetivo tiene vida completa
    if (objetivo.VidaActual_Entidad == objetivo.Vida_Entidad)
    {
        dano = (int)(dano * 1.25f); // +25% daño
    }
    
    return dano;
}
```

### Paladín
```csharp
// Características:
// - Estilo: Melee
// - Alta vida y defensa
// - Puede curarse
// - Bonus de daño contra No-Muertos

protected override int AplicarMitigacionPorFaccion(int danoBruto, ElementAttribute tipo)
{
    // Los paladines reciben menos daño de oscuridad
    if (tipo == ElementAttribute.Dark)
    {
        return (int)(danoBruto * 0.7f); // -30% daño oscuro
    }
    return danoBruto;
}

public override int CalcularDanoContra(IEntidadCombate objetivo)
{
    int dano = PuntosDeAtaque_Entidad;
    
    // Bonus contra no-muertos
    if (objetivo.EsTipoEntidad(TipoEntidades.NoMuerto))
    {
        dano = (int)(dano * 1.5f); // +50% daño
    }
    
    return dano;
}
```

### Asesino
```csharp
// Características:
// - Estilo: Melee
// - Alta velocidad y ataque
// - Baja vida
// - Probabilidad de crítico

private const float PROB_CRITICO = 0.25f;
private const int MULT_CRITICO = 2;

public override int CalcularDanoContra(IEntidadCombate objetivo)
{
    int dano = PuntosDeAtaque_Entidad;
    
    // 25% de probabilidad de crítico
    if (UnityEngine.Random.value < PROB_CRITICO)
    {
        dano *= MULT_CRITICO;
        Debug.Log(Nombre_Entidad + " realiza un golpe critico!");
    }
    
    // Bonus si el objetivo tiene poca vida
    float porcentajeVida = (float)objetivo.VidaActual_Entidad / objetivo.Vida_Entidad;
    if (porcentajeVida < 0.3f)
    {
        dano = (int)(dano * 1.3f); // +30% para rematar
    }
    
    return dano;
}
```

---

## Tabla Comparativa de Clases

| Clase | HP/Nv | ATK/Nv | DEF/Nv | MP/Nv | VEL/Nv | Estilo |
|-------|-------|--------|--------|-------|--------|--------|
| Guerrero | +15 | +4 | +3 | +3 | +1 | Melee |
| Mago | +8 | +2 | +1 | +10 | +2 | Ranged |
| Arquero | +10 | +5 | +1 | +4 | +3 | Ranged |
| Paladín | +18 | +3 | +4 | +5 | +0 | Melee |
| Asesino | +8 | +6 | +1 | +2 | +4 | Melee |
