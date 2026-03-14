# TODO - Estado del Proyecto Saclisam

> Última actualización: Marzo 2026

---

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
- [x] `EntityController.IsPlayerOwned` + `CharacterId`
- [x] `PlayerInterestZone` sigue al main dinámicamente
- [x] Eventos de party en EventBus

### Sistema de Cámara y Movimiento
- [x] `IsometricCameraController` - Cámara isométrica con zoom y rotación
- [x] `CameraSettings` - ScriptableObject con configuración
- [x] `GameInputManager` - Input híbrido WASD + Click
- [x] `PlayerMovementController` - Movimiento del Main con NavMesh
- [x] `PartyFollower` - Seguidores con separación anti-clipping

### Sistema de UI de Combate
- [x] `CombatUIController` - Controlador principal
- [x] `CombatActionMenu` - Menú de acciones
- [x] `SkillSelectionPanel` - Panel de selección de habilidades
- [x] `TargetSelector` - Selector de objetivos

### Sistema de Evoluciones (Data-Driven)
- [x] 14 condiciones como ScriptableObjects independientes
- [x] `EvolutionConditionSO` base abstracta con `Evaluar(EvolutionState)`
- [x] `TraitDefinition` y `TraitChainDefinition` con condiciones SO
- [x] `ClassEvolutionDefinition` con traits requeridos
- [x] `EvolutionState` per-personaje (serializable)
- [x] `EvolutionEvaluator` filtra disponibles per-personaje
- [x] `EvolutionController` suscrito al EventBus
- [x] `ModuloClaseSO` + `IComportamientoDeClase` para módulos
- [x] Editor custom para TraitChainDefinition

### Sistema de Misiones (Multi-Personaje)
- [x] `MissionDefinitionSO` con scope: Global/Personal/Exclusive
- [x] `MissionManager` orquestador 100% event-driven
- [x] 6 tipos de `MissionConditionSO` (evalúan contra EvolutionState)
- [x] `MissionObjectiveSO` + instancias runtime (Kill, Collect, Zone, Interact)
- [x] `CharacterMissionData` per-personaje
- [x] `MissionSaveData` con persistencia global + per-personaje
- [x] Participantes de combate: todo el party activo recibe crédito
- [x] Misiones exclusivas: asignación permanente a un personaje

### Estado Global y Per-Personaje
- [x] `GlobalPlayerState` - misiones globales, traits bloqueados, flags
- [x] `EvolutionState` per-personaje - contadores, traits, evoluciones
- [x] Integración: MissionManager registra personajes con EvolutionState
- [x] Traits `globalmenteUnico` registran qué personaje los desbloqueó

### Sistema de Selección de Personaje
- [x] `CharacterSelectionConfig` - SO con clases disponibles, prefab, límites
- [x] `CharacterSelectionManager` - Lógica de creación, registro y transición
- [x] `CharacterSelectionUI` - Controlador UI Toolkit completo
- [x] `CharacterSelectionBootstrap` - Setup de escena
- [x] `CharacterSelection.uxml` + `.uss` - Layout y estilos
- [x] Preview de clase con stats, habilidades y descripción
- [x] Integración con PlayerPartyManager + MissionManager + EvolutionState

### Integración Pooling + Chunks + Enemigos
- [x] `ObjectPool<T>` genérico thread-safe
- [x] `DynamicEnemyPoolManager` pools on-demand por tipo
- [x] `WorldChunkManager` carga/descarga por zona
- [x] `PersistentEnemyManager` estado por instancia

---

## 🔄 En Progreso

### Conectar EvolutionController al Flujo Completo
- [ ] Conectar `EvolutionController` a `PlayerPartyManager.OnMainChanged` para cambio de personaje activo
- [ ] `EvolutionApplier` aplicar efectos `AgregarModulo` correctamente
- [ ] Generar ofertas de evolución al subir de nivel
- [ ] Crear assets de prueba (Paladín, Heraldo, Emomancer)

---

## 📋 Pendientes por Fase

### Fase 1: Selección de Personaje
- [x] **Scripts de selección** (CharacterSelectionManager, UI, Config, Bootstrap)
- [x] **UXML + USS** (CharacterSelection.uxml + .uss)
- [ ] **Crear escena en Unity** (File > New Scene, agregar GameObjects + UIDocument)
- [ ] **Crear CharacterSelectionConfig.asset** (Resources/, asignar clases + prefab)
- [ ] Crear prefabs para Mago y Arquero (solo existe Guerrero.prefab)
- [ ] Agregar escena a Build Settings

### Fase 2: UI de Party y Personajes
- [ ] UI de party/switching en gameplay (cambiar main con tecla)
- [ ] Panel de estado de personaje (stats, traits, evoluciones)
- [ ] UI de refuerzos disponibles durante combate
- [ ] Menú de personajes estacionados
- [ ] UI de misiones activas per-personaje
- [ ] UI de misiones globales vs personales

### Fase 3: Integración SaveSystem con Multi-Personaje
- [ ] Implementar `SaveData` v2.0 con estructura per-personaje
- [ ] Serializar `EvolutionState` per-personaje
- [ ] Serializar `GlobalPlayerState`
- [ ] Flujo completo: guardar todos los personajes + misiones + global
- [ ] Flujo completo: cargar y restaurar party + registrar en managers
- [ ] Auto-guardado con estructura nueva

### Fase 4: Recompensas y Distribución
- [ ] Sistema de recompensas de misiones: `MissionRewardSO` concretos
- [ ] Distribución: Global → cuenta jugador, Personal → personaje
- [ ] `ExperienceRewardSO`, `CurrencyRewardSO`, `ItemRewardSO` → conectar
- [ ] Trait rewards (desbloquear trait como recompensa)
- [ ] Sistema de inventario (necesario para ItemReward)

### Fase 5: Mundo y Narrativa
- [ ] `WorldState` para estado de NPCs, zonas, facciones
- [ ] Sistema de NPCs con roles funcionales
- [ ] Consecuencias de misiones sobre el mundo
- [ ] Variantes de misión según estado del mundo
- [ ] Arcos narrativos de NPCs

### Fase 6: Gameplay Pendiente
- [ ] Sistema de inventario
- [ ] Sistema de diálogos/NPCs
- [ ] Sistema de tiendas
- [ ] Sistema de facciones y reputación
- [ ] Biomas visitados tracking en EvolutionState

### Fase 7: Audio/Visual
- [ ] Efectos visuales de habilidades
- [ ] Sonidos de combate
- [ ] Animaciones de personajes
- [ ] Indicador visual de click-to-move
- [ ] Highlight de turno de personaje

### Fase 8: Testing
- [ ] Tests unitarios de EvolutionEvaluator con múltiples EvolutionStates
- [ ] Tests de MissionManager (global/personal/exclusive flows)
- [ ] Tests de GlobalPlayerState (trait lock, mission assignment)
- [ ] Tests de combate y party management
- [ ] Tests de save/load con multi-personaje

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

### Reglas Multi-Personaje

| Tipo Misión | Quién progresa | Dónde se registra | Recompensas |
|-------------|----------------|-------------------|-------------|
| Global | Todos contribuyen | GlobalPlayerState + todos los EvolutionState | Cuenta jugador |
| Personal | Solo el personaje | CharacterMissionData + su EvolutionState | Al personaje |
| Exclusive | Personaje que la reclamó | CharacterMissionData + su EvolutionState | Al personaje |

### Archivos Clave del Sistema Multi-Personaje

| Archivo | Responsabilidad |
|---------|-----------------|
| `EvolutionState.cs` | Estado per-personaje (serializable) |
| `GlobalPlayerState.cs` | Estado compartido (traits únicos, misiones globales) |
| `MissionManager.cs` | Orquestador de misiones (3 scopes) |
| `CharacterMissionData.cs` | Datos de misión per-personaje |
| `PlayerPartyManager.cs` | Gestión de personajes (main/party/stationed) |
| `EvolutionController.cs` | Puente EventBus ↔ EvolutionState |
| `EvolutionEvaluator.cs` | Filtrado de traits/evoluciones disponibles |
