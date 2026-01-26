# Clases del Jugador

## Visión General

Las clases del jugador heredan de `Jugador` y definen comportamientos específicos como cálculo de daño personalizado.

```
Jugador (abstracta)
    └── Guerrero
    └── [Mago - futuro]
    └── [Arquero - futuro]
```

---

## Guerrero

**Archivo**: `Assets/Scripts/Subclases/Guerrero.cs`  
**Namespace**: `Subclases`

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
