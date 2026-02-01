# TODO - Estado del Proyecto

## ✅ Sistemas Completados

### Sistema de Combate Dinámico
- [x] `ICombatCandidate` - Interface para candidatos de combate
- [x] `CombatRules` - ScriptableObject con reglas configurables
- [x] `PlayerInterestZone` - Detección de enemigos por proximidad
- [x] `CombatEncounterManager` - Orquestación de encuentros
- [x] `CombateManager` - Refactorizado para soporte dinámico + UI
- [x] `EnemyController` - Implementa ICombatCandidate
- [x] Eventos de detección en EventBus

### Sistema de Party
- [x] `PlayerPartyManager` - Gestión de main, party activo, estacionados
- [x] `ReinforcementSystem` - Sistema de refuerzos con llegada por turnos
- [x] `EntityController.IsPlayerOwned` - Flag de ownership
- [x] `PlayerInterestZone` sigue al main dinámicamente
- [x] Eventos de party en EventBus

### Sistema de Cámara y Movimiento (NUEVO)
- [x] `IsometricCameraController` - Cámara isométrica con zoom y rotación
- [x] `CameraSettings` - ScriptableObject con configuración de cámara
- [x] `GameInputManager` - Input híbrido WASD + Click
- [x] `PlayerMovementController` - Movimiento del Main con NavMesh
- [x] `PartyFollower` - Seguidores con separación anti-clipping

### Sistema de UI de Combate (NUEVO)
- [x] `CombatUIController` - Controlador principal de UI de combate
- [x] `CombatActionMenu` - Menú de acciones (Atacar, Defender, Ceder Turno)
- [x] `SkillSelectionPanel` - Panel de selección de habilidades
- [x] `TargetSelector` - Selector de objetivos con indicadores
- [x] Eventos de UI en EventBus (EventoEsperandoAccionJugador, etc.)

### Integración
- [x] CombatEncounterManager usa PlayerPartyManager.ActiveParty
- [x] CombateManager.AgregarAliadoAlCombate para refuerzos
- [x] TurnManager.AgregarEntidad para entidades mid-combat
- [x] CombateManager espera input de UI para jugadores

---

## 🔄 En Progreso

### Sistema de Evoluciones
- [ ] Conectar EvolutionController al jugador actual y EventBus
- [ ] Completar EvolutionApplier para efectos pendientes
- [ ] Ajustar EvolutionEvaluator para estados reales
- [ ] Crear assets de pruebas (Paladín, Heraldo, Emomancer)
- [ ] Exponer la oferta a la UI

---

## 📋 Pendientes

### Gameplay
- [ ] Sistema de inventario
- [ ] Sistema de misiones
- [ ] Diálogos/NPCs
- [ ] Tiendas

### UI Pendientes
- [ ] Prefabs de UI (CombatActionMenu, SkillPanel, TargetIndicator)
- [ ] UI de party/switching
- [ ] UI de refuerzos disponibles
- [ ] Menú de personajes estacionados
- [ ] Crear CameraSettings.asset en Unity

### Audio/Visual
- [ ] Efectos visuales de habilidades
- [ ] Sonidos de combate
- [ ] Animaciones de personajes
- [ ] Indicador visual de click-to-move
- [ ] Highlight de turno de personaje

### Testing
- [ ] Tests unitarios de combate
- [ ] Tests de party management
- [ ] Tests de refuerzos

---

## 📝 Notas

### Configuración Recomendada

**CombatRules:**
- maxEnemiesPerEncounter: 5
- maxAlliesPerEncounter: 4 (party activo)
- autoStartCombat: true
- encounterCooldown: 3s

**PlayerPartyManager:**
- maxOwnedCharacters: 20
- maxActivePartySize: 5
- distancePerTurn: 20 (para refuerzos)

**CameraSettings:** (crear en Unity)
- pitchAngle: 45°
- defaultZoomDistance: 12
- followSmoothing: 8

### Flujo de Combate Actual

```
Exploración → Detección → Evaluación → Combate
                                          │
                           ┌──────────────┴──────────────┐
                           │                             │
                      Es Jugador?                   Es Enemigo
                           │                             │
              EventoEsperandoAccion              IA decide acción
                           │                             │
              Click personaje → Menú              Ejecutar acción
                           │
              Atacar/Defender/Ceder
                           │
              Seleccionar habilidad
                           │
              Seleccionar objetivo
                           │
              EventoObjetivoSeleccionado
                           │
                   Ejecutar → Fin turno
```

### Archivos Clave Nuevos
**Cámara y Movimiento:**
- `Assets/Scripts/Camera/IsometricCameraController.cs`
- `Assets/Scripts/Camera/CameraSettings.cs`
- `Assets/Scripts/Movement/PlayerMovementController.cs`
- `Assets/Scripts/Movement/PartyFollower.cs`
- `Assets/Scripts/Input/GameInputManager.cs`

**UI de Combate:**
- `Assets/Scripts/UI/Combat/CombatUIController.cs`
- `Assets/Scripts/UI/Combat/CombatActionMenu.cs`
- `Assets/Scripts/UI/Combat/SkillSelectionPanel.cs`
- `Assets/Scripts/UI/Combat/TargetSelector.cs`

**Eventos Nuevos:**
- `EventoEsperandoAccionJugador`
- `EventoAccionSeleccionada`
- `EventoObjetivoSeleccionado`
- `EventoAccionCancelada`
- `CombatActionType` enum

---

## 🎮 Setup en Unity

### Para Cámara Isométrica:
1. Crear `CameraSettings.asset`: Create > Saclisam > Camera Settings
2. Agregar `IsometricCameraController` a la cámara principal
3. Asignar el CameraSettings

### Para Movimiento:
1. Agregar `PlayerMovementController` a un GameObject vacío
2. Asegurarse que tiene NavMeshAgent
3. Configurar layers en GameInputManager (Ground, Entity, Enemy)

### Para Party Followers:
1. Agregar `PartyFollower` a cada miembro del party (excepto Main)
2. Asegurarse que tienen NavMeshAgent
3. Configurar formationIndex según orden

### Para UI de Combate:
1. Crear Canvas con CombatUIController
2. Crear prefabs para CombatActionMenu, SkillSelectionPanel, TargetSelector
3. Asignar referencias en el inspector
