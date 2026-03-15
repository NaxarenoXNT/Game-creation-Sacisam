# 32 — Consola de Desarrollador

## Resumen

Sistema de consola en runtime para debug y testing de gameplay.
Permite ejecutar comandos tipo Minecraft (`spawn goblin 3`, `heal`, `help`)
sin modificar los sistemas del juego.

---

## Arquitectura

```
Console UI (captura input, muestra output)
    ↓
CommandParser (parsea string → nombre + args)
    ↓
CommandRegistry (busca comando registrado, ejecuta con try/catch)
    ↓
ICommand (clase independiente por comando)
    ↓
GameContext (inyecta sistemas del juego via interfaces)
    ↓
Sistemas reales (WorldChunkManager, PlayerPartyManager, etc.)
```

### Principios

- **UI desacoplada**: `ConsoleUI` solo captura texto y muestra resultados.
  No ejecuta lógica de gameplay.
- **Comandos independientes**: cada comando es una clase que implementa `ICommand`.
  No hay bloques `if/else` centralizados.
- **Sin excepciones al usuario**: los comandos retornan `CommandResult.Ok/Fail`.
  Errores inesperados son capturados por el `CommandRegistry`.
- **Data-driven**: los comandos usan IDs de string (`"goblin_archer"`)
  resueltos por los sistemas del juego, nunca por acceso directo a assets.

---

## Estructura de Archivos

```
Assets/Scripts/Console/
├── Core/
│   ├── ICommand.cs            — Interfaz base de comandos
│   ├── CommandResult.cs       — Struct Ok/Fail retornado por cada comando
│   ├── CommandParser.cs       — Parsea "spawn goblin 3" → ("spawn", ["goblin","3"])
│   └── CommandRegistry.cs     — Registro central + dispatch + try/catch
├── Context/
│   ├── GameContext.cs          — Contenedor de interfaces de sistemas
│   ├── IEnemySpawner.cs       — Contrato: Spawn, EnemyExists, KillAll
│   ├── IInventorySystem.cs    — Contrato: GiveItem, ItemExists (solo interfaz)
│   └── IPlayerProgression.cs  — Contrato: Level, Health, LevelUp, HealToFull
├── Adapters/
│   ├── EnemySpawnerAdapter.cs — Conecta IEnemySpawner → WorldChunkManager + Pool
│   └── PlayerProgressionAdapter.cs — Conecta IPlayerProgression → PlayerPartyManager
├── Commands/
│   ├── SpawnCommand.cs         — spawn <enemyID> [count]
│   ├── GiveCommand.cs          — give <itemID> [amount]
│   ├── LevelUpCommand.cs       — levelup [amount]
│   ├── HealCommand.cs          — heal
│   ├── KillAllCommand.cs       — killall
│   └── HelpCommand.cs          — help
├── UI/
│   └── ConsoleUI.cs            — Panel UI con input, output, historial
└── ConsoleBootstrapper.cs      — Crea GameContext, registra comandos, inicializa UI
```

---

## Flujo de Ejecución

### Ejemplo: `spawn goblin 3`

1. **ConsoleUI** captura el texto `"spawn goblin 3"` al presionar Enter.
2. Llama a `CommandRegistry.ExecuteRaw("spawn goblin 3")`.
3. **CommandParser** separa: `commandName = "spawn"`, `args = ["goblin", "3"]`.
4. **CommandRegistry** busca `"spawn"` en su diccionario → encuentra `SpawnCommand`.
5. Ejecuta `SpawnCommand.Execute(["goblin", "3"], gameContext)` dentro de un try/catch.
6. **SpawnCommand** valida argumentos, llama a `context.EnemySpawner.Spawn("goblin", 3)`.
7. **EnemySpawnerAdapter** resuelve `"goblin"` buscando en su diccionario de `EnemigoData`
   cargados desde `Resources/EnemigosData/`. Usa `DynamicEnemyPoolManager` para obtener
   controllers y los posiciona cerca del jugador.
8. Retorna `CommandResult.Ok("Spawned 3 goblin.")`.
9. **ConsoleUI** muestra el mensaje en pantalla.

---

## Conexión con Sistemas Existentes

### IEnemySpawner → EnemySpawnerAdapter

| Método Consola         | Sistema Real                                          |
|------------------------|-------------------------------------------------------|
| `EnemyExists(id)`      | Busca en diccionario de `EnemigoData` por `nombreEnemigo` |
| `Spawn(id, count)`     | `DynamicEnemyPoolManager.ObtenerController(data)` + posiciona cerca del jugador |
| `KillAll()`            | Itera `WorldChunkManager` chunks cargados → mata enemigos activos y los devuelve al pool |

**Resolución de IDs**: Los `EnemigoData` se cargan con `Resources.LoadAll<EnemigoData>("EnemigosData")`
al inicializar el adapter. Se indexan por `nombreEnemigo.ToLowerInvariant()`.
El nombre del asset (`Goblin_Data.asset`) es irrelevante; lo que importa es el campo `nombreEnemigo`.

### IPlayerProgression → PlayerProgressionAdapter

| Método Consola     | Sistema Real                                                    |
|--------------------|-----------------------------------------------------------------|
| `CurrentLevel`     | `PlayerPartyManager.Instance.MainCharacter.Nivel_Entidad`      |
| `CurrentHealth`    | `MainCharacter.VidaActual_Entidad`                              |
| `MaxHealth`        | `MainCharacter.Vida_Entidad`                                    |
| `LevelUp(amount)`  | Llama `SubirNivel()` N veces en la `Jugador` lógica interna    |
| `HealToFull()`     | `MainCharacter.Curar(MaxHealth)` — cura al máximo               |

### IInventorySystem

**No implementado todavía.** Solo existe la interfaz.
El `GiveCommand` detecta `context.InventorySystem == null` y retorna error gracefully.

---

## Comandos Disponibles

| Comando                   | Descripción                               | Ejemplo               |
|---------------------------|-------------------------------------------|-----------------------|
| `spawn <id> [count]`     | Spawnea enemigos por ID                   | `spawn goblin 5`     |
| `give <id> [amount]`     | Da items al jugador (requiere inventario) | `give potion 10`     |
| `levelup [amount]`       | Sube de nivel al personaje principal      | `levelup 5`          |
| `heal`                   | Restaura vida al máximo                   | `heal`               |
| `killall`                | Mata todos los enemigos activos           | `killall`            |
| `help`                   | Lista todos los comandos registrados      | `help`               |

---

## Manejo de Errores

Los comandos **nunca lanzan excepciones** para errores de usuario.

```
> spawn dragon
Error: enemy "dragon" not found. Available: goblin, orco, dragon_data

> spawn goblin abc
Error: count must be a valid integer greater than 0.

> give potion 10
Error: inventory system is not available.

> levelup -5
Error: amount must be a valid positive integer.
```

Si un comando lanza una excepción inesperada, el `CommandRegistry` la captura:
```
> spawn goblin 1
[Error] Internal error: NullReferenceException...
```

---

## Agregar un Nuevo Comando

1. Crear clase en `Console/Commands/`:

```csharp
using Console.Core;
using Console.Context;

namespace Console.Commands
{
    public class TeleportCommand : ICommand
    {
        public string Name => "teleport";
        public string Description => "teleport <x> <y> <z> - Teleports the player.";

        public CommandResult Execute(string[] args, GameContext context)
        {
            // validar args, ejecutar via context
            return CommandResult.Ok("Teleported.");
        }
    }
}
```

2. Registrar en `ConsoleBootstrapper.RegisterCommands()`:

```csharp
registry.Register(new TeleportCommand());
```

No se modifica ningún otro archivo del sistema de consola.

---

## Setup en Unity

### Automático (recomendado)

`ConsoleBootstrapper` auto-detecta los sistemas del juego en `Start()`:
- `WorldChunkManager.Instance` → crea `EnemySpawnerAdapter`
- `PlayerPartyManager.Instance.MainCharacter` → crea `PlayerProgressionAdapter`
- `DynamicEnemyPoolManager.Instance` → usado por el adapter de enemigos

Solo necesitás:
1. Crear un GameObject **"DevConsole"** en la escena.
2. Agregar el componente `ConsoleBootstrapper`.
3. Asignar la referencia a `ConsoleUI` (componente en el Canvas de la consola).
4. Crear el Canvas de consola con: Panel, InputField, Text (output), ScrollRect.

### Tecla de Toggle

Por defecto: **` (backtick/tilde)**. Configurable en el Inspector de `ConsoleUI`.

---

## Consideraciones de Producción

- El sistema completo debería ir envuelto en `#if UNITY_EDITOR || DEVELOPMENT_BUILD`
  para excluirlo de builds de release.
- Los adapters no modifican los sistemas originales. Son wrappers de solo lectura
  (excepto spawn/kill que son operaciones de debug).
- El `EnemySpawnerAdapter` spawnea enemigos **fuera del sistema de chunks**.
  Estos enemigos no tienen `spawnId` ni tracking de derrota persistente.
  Son puramente para testing.
