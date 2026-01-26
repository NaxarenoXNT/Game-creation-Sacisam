# Sistema Elemental

## Visión General

El sistema elemental define las ventajas y desventajas entre tipos de daño, similar a Pokémon.

```
ElementDefinition (ScriptableObject)
        │
        ├── tipoElemento (ElementFlag)
        ├── fortalezas[] (ElementFlag[])
        ├── debilidades[] (ElementFlag[])
        └── multiplicadorCritico
```

---

## ElementFlag (Enum)

**Archivo**: `Assets/Scripts/Flags/Tipo.cs`

```csharp
[Flags]
public enum ElementFlag
{
    None = 0,
    Fire = 1,      // Fuego
    Water = 2,     // Agua
    Earth = 4,     // Tierra
    Wind = 8,      // Viento
    Light = 16,    // Luz
    Dark = 32,     // Oscuridad
    Physical = 64  // Físico (sin elemento)
}
```

---

## ElementDefinition (ScriptableObject)

**Ubicación**: `Assets/Resources/Elements/`

### Crear Elemento

1. **Assets → Create → RPG → Element Definition**
2. Configurar propiedades

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `nombreElemento` | string | Nombre para mostrar |
| `tipoElemento` | ElementFlag | El tipo de elemento |
| `fortalezas` | ElementFlag[] | Elementos contra los que es fuerte |
| `debilidades` | ElementFlag[] | Elementos contra los que es débil |
| `multiplicadorCritico` | float | Multiplicador de daño crítico |

---

## Triángulo Elemental Básico

```
        Fuego
       /     \
      /   ↑   \
     /         \
   Tierra ←──── Agua
```

### Relaciones

| Atacante | Fuerte Contra | Débil Contra |
|----------|---------------|--------------|
| Fire | Earth, Wind | Water |
| Water | Fire | Earth, Wind |
| Earth | Water, Wind | Fire |
| Wind | Water | Earth, Fire |
| Light | Dark | Dark |
| Dark | Light | Light |

---

## EntityStats

**Archivo**: `Assets/Scripts/Padres/EntityStats.cs`

### Propiedades

```csharp
public class EntityStats
{
    // Stats base
    public int VidaMaxima { get; set; }
    public int VidaActual { get; set; }
    public int Ataque { get; set; }
    public int Defensa { get; set; }
    public int Magia { get; set; }
    public int Velocidad { get; set; }
    
    // Elemento
    public ElementFlag Elemento { get; set; }
}
```

### Método de Cálculo de Daño

```csharp
public int CalcularDanoContra(ElementFlag elementoAtaque, int danoBase, bool esMagico)
{
    float multiplicador = 1.0f;
    
    // Cargar definiciones de elementos
    var elementDefs = Resources.LoadAll<ElementDefinition>("Elements");
    
    foreach (var def in elementDefs)
    {
        if (def.tipoElemento == elementoAtaque)
        {
            // Verificar si el defensor es débil
            foreach (var debilidad in def.fortalezas)
            {
                if ((this.Elemento & debilidad) != 0)
                {
                    multiplicador = 1.5f; // Daño aumentado
                    break;
                }
            }
            
            // Verificar si el defensor resiste
            foreach (var resistencia in def.debilidades)
            {
                if ((this.Elemento & resistencia) != 0)
                {
                    multiplicador = 0.5f; // Daño reducido
                    break;
                }
            }
            break;
        }
    }
    
    int defensa = esMagico ? Magia / 2 : Defensa;
    int danoFinal = Mathf.Max(1, (int)((danoBase - defensa) * multiplicador));
    
    return danoFinal;
}
```

---

## Configurar Elementos en Unity

### Paso 1: Crear ElementDefinition

1. **Project → Assets/Resources/Elements**
2. **Clic derecho → Create → RPG → Element Definition**
3. Nombrar: "Fire", "Water", "Earth", etc.

### Paso 2: Configurar Fire

```
nombreElemento: "Fuego"
tipoElemento: Fire
fortalezas: [Earth, Wind]   ← Es fuerte contra
debilidades: [Water]        ← Es débil contra
multiplicadorCritico: 1.5
```

### Paso 3: Configurar Water

```
nombreElemento: "Agua"
tipoElemento: Water
fortalezas: [Fire]
debilidades: [Earth, Wind]
multiplicadorCritico: 1.5
```

### Paso 4: Configurar Earth

```
nombreElemento: "Tierra"
tipoElemento: Earth
fortalezas: [Water, Wind]
debilidades: [Fire]
multiplicadorCritico: 1.5
```

---

## Uso en Habilidades

### HabilidadData

```csharp
[CreateAssetMenu(fileName = "NuevaHabilidad", menuName = "RPG/Habilidad")]
public class HabilidadData : ScriptableObject
{
    public ElementFlag tipoDano;  // Elemento del daño
    public int potencia;          // Daño base
    public bool esMagico;         // ¿Usa Magia o Ataque?
    // ...
}
```

### Ejemplo: Bola de Fuego

```
tipoDano: Fire
potencia: 25
esMagico: true
```

### Ejemplo: Estocada

```
tipoDano: Physical
potencia: 15
esMagico: false
```

---

## Uso en DamageEffect

**Archivo**: `Assets/Scripts/Habilidades/DamageEffect.cs`

```csharp
public void Aplicar(Entidad objetivo, Entidad lanzador, int potencia, bool esMagico)
{
    // Obtener el elemento del lanzador o la habilidad
    ElementFlag elementoAtaque = ObtenerElementoAtaque();
    
    // Calcular daño con modificador elemental
    int dano = objetivo.Stats.CalcularDanoContra(elementoAtaque, potencia, esMagico);
    
    // Aplicar el daño
    objetivo.RecibirDano(dano);
    
    Debug.Log($"[Daño] {lanzador.Nombre} → {objetivo.Nombre}: {dano} ({elementoAtaque})");
}
```

---

## Asignar Elemento a Entidades

### En ClaseData (Jugador)

```csharp
[CreateAssetMenu(fileName = "NuevaClase", menuName = "RPG/Clase Jugador")]
public class ClaseData : ScriptableObject
{
    public ElementFlag elementoBase = ElementFlag.Physical;
    // ...
}
```

### En EnemigoData (Enemigos)

```csharp
[CreateAssetMenu(fileName = "NuevoEnemigo", menuName = "RPG/Enemigo")]
public class EnemigoData : ScriptableObject
{
    public ElementFlag elemento = ElementFlag.Physical;
    // ...
}
```

---

## Elementos Combinados

El sistema soporta elementos combinados usando flags:

```csharp
// Entidad con dos elementos
EntityStats stats = new EntityStats
{
    Elemento = ElementFlag.Fire | ElementFlag.Dark  // Fuego + Oscuridad
};
```

Esto permite crear enemigos o clases híbridas.

---

## Tabla de Multiplicadores

| Condición | Multiplicador | Efecto |
|-----------|---------------|--------|
| Neutro | 1.0x | Daño normal |
| Fuerte contra | 1.5x | "¡Es muy efectivo!" |
| Débil contra | 0.5x | "No es muy efectivo..." |
| Mismo elemento | 0.75x | Resistencia natural |

---

## Mostrar en UI

```csharp
// Obtener color según elemento
public static Color ObtenerColorElemento(ElementFlag elemento)
{
    switch (elemento)
    {
        case ElementFlag.Fire: return Color.red;
        case ElementFlag.Water: return Color.blue;
        case ElementFlag.Earth: return new Color(0.6f, 0.4f, 0.2f);
        case ElementFlag.Wind: return Color.cyan;
        case ElementFlag.Light: return Color.yellow;
        case ElementFlag.Dark: return new Color(0.3f, 0f, 0.3f);
        default: return Color.gray;
    }
}
```

---

## Ejemplo Completo de Combate

```
Mago (Fire) usa Bola de Fuego → Slime de Agua (Water)

1. tipoDano: Fire
2. potencia: 25
3. Verificar: Fire es débil contra Water
4. multiplicador: 0.5x
5. Daño final: 25 * 0.5 = 12

→ "¡No es muy efectivo!"
```

```
Mago (Fire) usa Bola de Fuego → Golem de Tierra (Earth)

1. tipoDano: Fire
2. potencia: 25
3. Verificar: Fire es fuerte contra Earth
4. multiplicador: 1.5x
5. Daño final: 25 * 1.5 = 37

→ "¡Es súper efectivo!"
```
