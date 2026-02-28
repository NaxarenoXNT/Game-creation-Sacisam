# Stack-Based Game Flow Controller

## 1. Objetivo

Implementar un sistema centralizado de gestión de modos globales del juego que:

- Desacople los sistemas (Combat, Dialogue, Inventory, Exploration).
- Permita transiciones limpias entre estados.
- Soporte estados exclusivos y superpuestos.
- Evite `if(GameState == X)` distribuidos por el código.
- Mantenga bajo acoplamiento y alta cohesión.

> Este sistema no contiene lógica de gameplay. Solo orquesta modos de ejecución.

---

## 2. Concepto Arquitectónico

> El juego no cambia de mundo. Cambia quién tiene el control.

Por eso usamos un **Stack-Based Flow Controller**.

El stack permite:

- Reemplazo de modos (`Exploration → Combat`).
- Superposición de modos (`Combat → Pause`).
- Restauración automática al hacer `Pop()`.

---

## 3. Contrato del Estado

```csharp
public interface IGameFlowState
{
    void Enter();
    void Exit();
    bool BlocksLowerStates { get; }
}
```

**Responsabilidades del estado:**

- Activar/desactivar sistemas.
- Configurar input.
- Mostrar/ocultar UI.
- Suscribirse/desuscribirse al EventBus.
- Gestionar su propio ciclo de vida.

> El estado se autogestiona. El FlowController no conoce detalles internos.

---

## 4. Implementación del FlowController

```csharp
public class GameFlowController
{
    private readonly Stack<IGameFlowState> _stateStack = new();

    public void Push(IGameFlowState state)
    {
        if (_stateStack.Count > 0 && state.BlocksLowerStates)
        {
            _stateStack.Peek().Exit();
        }

        _stateStack.Push(state);
        state.Enter();
    }

    public void Pop()
    {
        if (_stateStack.Count == 0)
            return;

        var current = _stateStack.Pop();
        current.Exit();

        if (_stateStack.Count > 0)
        {
            _stateStack.Peek().Enter();
        }
    }
}
```

---

## 5. Validación de Transiciones

Antes de escalar a una tabla de transiciones compleja, conviene agregar desde el día uno una capa mínima de validación directamente en el FlowController.

### 5.1 Actualización del contrato

```csharp
public interface IGameFlowState
{
    void Enter();
    void Exit();
    bool BlocksLowerStates { get; }
    IEnumerable<Type> AllowedTransitions { get; }
}
```

Cada estado declara explícitamente a qué otros estados puede transicionar.

### 5.2 FlowController con validación

```csharp
public class GameFlowController
{
    private readonly Stack<IGameFlowState> _stateStack = new();

    public void Push(IGameFlowState state)
    {
        if (!IsTransitionAllowed(state))
        {
            Debug.LogWarning($"Transition to {state.GetType().Name} blocked");
            return;
        }

        if (_stateStack.Count > 0 && state.BlocksLowerStates)
        {
            _stateStack.Peek().Exit();
        }

        _stateStack.Push(state);
        state.Enter();
    }

    public void Pop()
    {
        if (_stateStack.Count == 0)
            return;

        var current = _stateStack.Pop();
        current.Exit();

        if (_stateStack.Count > 0)
        {
            _stateStack.Peek().Enter();
        }
    }

    private bool IsTransitionAllowed(IGameFlowState incoming)
    {
        if (_stateStack.Count == 0) return true;
        return _stateStack.Peek().AllowedTransitions.Contains(incoming.GetType());
    }
}
```

### 5.3 Ejemplo en un estado

```csharp
public class ExplorationState : IGameFlowState
{
    public bool BlocksLowerStates => true;

    public IEnumerable<Type> AllowedTransitions => new[]
    {
        typeof(CombatState),
        typeof(InventoryState),
        typeof(DialogueState),
        typeof(PauseState)
    };

    public void Enter() { /* activar sistemas de exploración */ }
    public void Exit()  { /* desactivar sistemas de exploración */ }
}
```

### 5.4 Por qué hacerlo desde el día uno

- Cada estado es dueño de sus propias reglas de salida.
- No hay lógica condicional distribuida.
- Cuando el proyecto crezca, esta estructura es la base natural para migrar a una tabla de transiciones formal sin reescribir nada.
- Los errores de transición inválida aparecen como warnings claros en lugar de bugs silenciosos.

> **Regla:** si un estado no declara una transición, simplemente no ocurre. El sistema falla de forma explícita y controlada.

---

## 6. Tipos de Estados

### 5.1 Estados Exclusivos

Reemplazan completamente el modo anterior.

**Ejemplo:** `Exploration → Combat`

```csharp
public bool BlocksLowerStates => true;
```

### 5.2 Estados Superpuestos

Se apilan encima del actual sin destruirlo.

**Ejemplos:** `Combat → Pause` / `Exploration → Inventory`

```csharp
public bool BlocksLowerStates => true; // bloquea input pero no destruye contexto
```

O si querés permitir coexistencia parcial:

```csharp
public bool BlocksLowerStates => false;
```

> Depende del diseño.

---

## 7. Flujo Típico

### Emboscada

```
Push(new CombatState());
```
Exploration sale automáticamente si Combat bloquea.

### Abrir inventario durante combate

```
Push(new InventoryState());
```
Combat queda congelado debajo.

### Cerrar inventario

```
Pop();
```
Combat vuelve a activarse.

---

## 8. Integración con EventBus

El FlowController debe escuchar eventos de alto nivel:

- `CombatRequested`
- `DialogueStarted`
- `ShopOpened`
- `InventoryOpened`
- `CombatEnded`

**Ejemplo conceptual:**

```
OnCombatRequested → Push(CombatState)
OnCombatEnded     → Pop()
```

> **Regla importante:** Solo el FlowController decide transiciones. Los sistemas no deben manipular el stack directamente.

---

## 9. Reglas de Diseño Importantes

### 9.1 No convertirlo en un God Object

El FlowController:

- No contiene lógica de combate.
- No valida reglas.
- No consulta condiciones internas.
- No conoce implementación de sistemas.

**Es un router de modos.**

### 9.2 Cada estado debe ser autocontenido

Un estado debe:

- Desuscribirse correctamente en `Exit()`.
- Restaurar input.
- Restaurar UI.
- No dejar efectos secundarios.

> Si un estado no limpia correctamente, el stack no te salva.

### 9.3 Definir reglas de transición

No todos los estados deberían poder:

- Interrumpir combate.
- Apilarse encima de pausa.
- Reemplazar ciertos modos.

Conviene definir una política de transición:

- Qué puede apilarse.
- Qué reemplaza.
- Qué se bloquea.

> Esto puede evolucionar a una tabla de reglas si el sistema crece.

---

## 10. Limitaciones

### 10.1 Complejidad creciente

Con muchos estados combinables, el stack puede volverse difícil de razonar si no hay reglas claras.

### 10.2 Transiciones ambiguas

El stack no entiende intención, solo orden.

```
Push(A) → Push(B) → Pop()
```

Siempre vuelve a `A`, aunque tal vez no sea lo que querías conceptualmente.

### 10.3 No modela sistemas permanentes

No usar Flow para:

- Clima.
- Música dinámica.
- Sistemas de hambre.
- IA global continua.

> Eso pertenece a sistemas independientes.

---

## 11. Buenas Prácticas

- Mantener estados pequeños y específicos.
- No mezclar lógica interna con transición.
- Centralizar decisiones en el FlowController.
- Evitar enums globales.
- Evitar consultas tipo `if(CurrentMode == X)` distribuidas.

---

## 12. Escalabilidad Futura

El sistema puede evolucionar hacia:

- FSM jerárquica.
- Políticas de transición formales.
- Prioridades de estados.
- Contextos de ejecución desacoplados.

> Para una arquitectura limpia en un RPG de mundo libre, este enfoque es suficiente y profesional.

---

## 13. Conclusión

Un Stack-Based FlowController:

- ✔ Centraliza el control de modos
- ✔ Desacopla sistemas
- ✔ Evita lógica condicional distribuida
- ✔ Permite overlays y bloqueos
- ✔ Escala con complejidad moderada

**Siempre que:**

- No contenga lógica de gameplay.
- Los estados sean autocontenidos.
- Las reglas de transición estén definidas.

---

## 14. Estado de Implementación ✅

**Implementado** con los siguientes archivos:

```
Assets/Scripts/GameFlow/
├── IGameFlowState.cs              # Interfaz: Enter(), Exit(), BlocksLowerStates, AllowedTransitions
├── GameFlowController.cs          # Singleton MonoBehaviour con stack + EventBus
└── States/
    ├── ExplorationFlowState.cs    # Estado base: configura InputContext.Exploration
    └── CombatFlowState.cs         # Estado de combate: configura InputContext.Combat

Assets/Scripts/Events/
└── EventosGameFlow.cs             # EventoGameFlowChanged
```

### Flujo implementado

```
[Start] → Push(ExplorationFlowState)
    ↓
EventoEncounterIniciado → Push(CombatFlowState)
    → CombatFlowState.Enter() → SetContext(Combat)
    → ExplorationFlowState.Exit() (BlocksLowerStates = true)
    ↓
EventoCombateFinalizado → Pop()
    → CombatFlowState.Exit()
    → ExplorationFlowState.Enter() → SetContext(Exploration)
```

### Migración realizada

- `CombatUIController` ya **no** cambia `InputContext` directamente.
- El cambio de contexto de input es responsabilidad exclusiva de los `IGameFlowState`.
- `CombatEncounterManager` no requirió cambios: ya publicaba `EventoEncounterIniciado`.
- Se agregó `EventoGameFlowChanged` al EventBus para que sistemas externos consulten el estado.

### Para agregar nuevos estados

1. Crear clase que implemente `IGameFlowState` en `GameFlow/States/`.
2. Agregar el tipo a `AllowedTransitions` de los estados que pueden transicionar a él.
3. Suscribir el evento correspondiente en `GameFlowController` para hacer `Push`/`Pop`.

Ejemplo para inventario:
```csharp
public class InventoryFlowState : IGameFlowState
{
    public bool BlocksLowerStates => true;
    public IEnumerable<Type> AllowedTransitions => new[] { typeof(ExplorationFlowState) };
    public void Enter() { GameInputManager.Instance.SetContext(InputContext.Menu); }
    public void Exit()  { /* limpiar UI inventario */ }
}
```