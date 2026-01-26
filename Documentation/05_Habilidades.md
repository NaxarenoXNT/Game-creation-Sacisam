# Sistema de Habilidades

## Visión General

Las habilidades se definen como **ScriptableObjects** (`HabilidadData`) que implementan el patrón **Command**. Cada habilidad contiene una lista de **efectos** que se ejecutan en secuencia.

```
HabilidadData (ScriptableObject, IHabilidadesCommand)
    │
    ├── nombreHabilidad
    ├── costeMana
    ├── cooldownTurnos
    ├── tipoObjetivo
    │
    └── efectos[] ──► DamageEffect
                  ──► HealEffect
                  ──► StatusEffect
```

---

## HabilidadData

**Archivo**: `Assets/Scripts/SO/HabilidadData.cs`

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `nombreHabilidad` | string | Nombre para mostrar |
| `icono` | Sprite | Icono de la habilidad |
| `descripcion` | string | Texto descriptivo |
| `costeMana` | int | Mana requerido |
| `cooldownTurnos` | int | Turnos de espera después de usar |
| `tipoObjetivo` | TargetType | A quién puede afectar |
| `efectos` | List<IHabilidadEffect> | Lista de efectos |

### Tipos de Objetivo (TargetType)

```csharp
public enum TargetType
{
    EnemigoUnico,    // Un enemigo específico
    EnemigoTodos,    // Todos los enemigos
    AliadoUnico,     // Un aliado específico
    AliadoTodos,     // Todos los aliados
    Self             // Solo el usuario
}
```

### Método Principal

```csharp
public void Ejecutar(
    Entidad invocador, 
    Entidad objetivoPrincipal, 
    List<IEntidadCombate> aliados, 
    List<IEntidadCombate> enemigos
)
{
    // Determinar objetivos según tipo
    List<Entidad> objetivos = ObtenerObjetivos(objetivoPrincipal, invocador, aliados, enemigos);
    
    // Aplicar cada efecto a cada objetivo
    foreach (Entidad objetivo in objetivos)
    {
        foreach (IHabilidadEffect efecto in efectos)
        {
            efecto.Aplicar(invocador, objetivo, aliados, enemigos);
        }
    }
}
```

### Verificar Viabilidad

```csharp
public bool EsViable(
    IEntidadCombate invocador, 
    IEntidadCombate objetivo, 
    List<IEntidadCombate> aliados, 
    List<IEntidadCombate> enemigos
)
{
    // Verificar que hay objetivo válido
    if (objetivo == null || !objetivo.EstaVivo())
        return false;
    
    // Verificar mana si es jugador
    if (invocador is IJugadorProgresion jugador)
    {
        if (jugador.ManaActual_jugador < costeMana)
            return false;
    }
    
    return true;
}
```

---

## Crear Habilidades en Unity

### Habilidad de Ataque Básico

1. **Click derecho** > Create > Combate > Habilidad Data
2. **Configurar**:
   ```
   Nombre Habilidad: "Ataque"
   Descripcion: "Un golpe físico básico"
   Coste Mana: 0
   Cooldown Turnos: 0
   Tipo Objetivo: EnemigoUnico
   ```
3. **Añadir efecto** (click en +):
   - Tipo: DamageEffect
   - Base Damage: 0
   - Tipo Dano: None

### Habilidad de Fuego

```
Nombre Habilidad: "Bola de Fuego"
Descripcion: "Lanza una bola de fuego ardiente"
Coste Mana: 15
Cooldown Turnos: 2
Tipo Objetivo: EnemigoUnico

Efectos:
  [0] DamageEffect
      - baseDamage: 25
      - tipoDano: Fire
```

### Habilidad en Área

```
Nombre Habilidad: "Terremoto"
Descripcion: "Sacude la tierra dañando a todos los enemigos"
Coste Mana: 30
Cooldown Turnos: 4
Tipo Objetivo: EnemigoTodos  ← Importante

Efectos:
  [0] DamageEffect
      - baseDamage: 15
      - tipoDano: Earth
```

### Habilidad de Curación

```
Nombre Habilidad: "Curar"
Descripcion: "Restaura vida al objetivo"
Coste Mana: 12
Cooldown Turnos: 2
Tipo Objetivo: AliadoUnico

Efectos:
  [0] HealEffect
      - healAmount: 40
      - porcentajeVidaMax: false
```

### Habilidad con Múltiples Efectos

```
Nombre Habilidad: "Golpe Venenoso"
Descripcion: "Golpea y envenena al objetivo"
Coste Mana: 10
Cooldown Turnos: 3
Tipo Objetivo: EnemigoUnico

Efectos:
  [0] DamageEffect
      - baseDamage: 10
      - tipoDano: None
  
  [1] StatusEffect
      - statusAplicar: Poisoned
      - duracionTurnos: 3
      - danoPorTurno: 5
      - modificadorStats: 0
```

### Habilidad de Buff

```
Nombre Habilidad: "Bendición"
Descripcion: "Aumenta las stats del aliado"
Coste Mana: 20
Cooldown Turnos: 5
Tipo Objetivo: AliadoUnico

Efectos:
  [0] StatusEffect
      - statusAplicar: Buffed  ← Estado positivo
      - duracionTurnos: 3
      - danoPorTurno: 0
      - modificadorStats: 0.2  ← +20% stats
```

---

## Interfaz IHabilidadesCommand

```csharp
public interface IHabilidadesCommand
{
    void Ejecutar(
        Entidad invocador, 
        Entidad objetivoPrincipal, 
        List<IEntidadCombate> aliados, 
        List<IEntidadCombate> enemigos
    );
    
    bool EsViable(
        IEntidadCombate invocador, 
        IEntidadCombate objetivo, 
        List<IEntidadCombate> aliados, 
        List<IEntidadCombate> enemigos
    );
}
```

---

## Flujo de Ejecución

```
┌─────────────────────────────────────────────────────┐
│              CombateManager                          │
│  EjecutarTurno(entidad)                             │
└─────────────────┬───────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────┐
│         IEntidadActuable                            │
│  ObtenerAccionElegida(aliados, enemigos)            │
│  → Retorna (HabilidadData, objetivo)                │
└─────────────────┬───────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────┐
│         HabilidadData.EsViable()                    │
│  - Verificar objetivo vivo                          │
│  - Verificar mana suficiente                        │
│  - Verificar cooldown (via GestorCooldowns)         │
└─────────────────┬───────────────────────────────────┘
                  │ Si es viable
                  ▼
┌─────────────────────────────────────────────────────┐
│         Consumir Recursos                           │
│  - Jugador.ConsumirMana(costeMana)                  │
│  - GestorCooldowns.IniciarCooldown(habilidad)       │
└─────────────────┬───────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────┐
│         HabilidadData.Ejecutar()                    │
│  - ObtenerObjetivos() según tipoObjetivo            │
│  - Para cada objetivo:                              │
│      - Para cada efecto:                            │
│          - efecto.Aplicar(inv, obj, ali, ene)       │
└─────────────────────────────────────────────────────┘
```

---

## Tabla de Habilidades Sugeridas

| Nombre | Mana | CD | Objetivo | Efectos |
|--------|------|----|---------| --------|
| Ataque | 0 | 0 | EnemigoUnico | Damage(0) |
| Golpe Fuerte | 5 | 1 | EnemigoUnico | Damage(15) |
| Bola de Fuego | 15 | 2 | EnemigoUnico | Damage(25, Fire) |
| Terremoto | 30 | 4 | EnemigoTodos | Damage(15, Earth) |
| Curar | 12 | 2 | AliadoUnico | Heal(40) |
| Curar Grupo | 25 | 4 | AliadoTodos | Heal(25) |
| Veneno | 8 | 3 | EnemigoUnico | Status(Poison, 3t, 5dmg) |
| Aturdimiento | 15 | 4 | EnemigoUnico | Status(Stunned, 1t) |
| Bendición | 20 | 5 | AliadoUnico | Status(Buffed, 3t, +20%) |
| Furia | 10 | 3 | Self | Status(Buffed, 2t, +30%) |
