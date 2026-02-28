# 🎮 Guía de Setup para Testing - Sistema de Enemigos y Combate

Esta guía te ayudará a configurar una escena de prueba completa para testear el sistema de enemigos, chunks y combate.

---

## ✅ Checklist Rápido

Antes de darle Play, asegúrate de tener:

- [ ] ✅ **GameConfig.asset** en `Assets/Resources/`
- [ ] ✅ **CombatRules.asset** en `Assets/Resources/`
- [ ] ✅ **5 Managers** en la Hierarchy
- [ ] ✅ **Player** configurado correctamente
- [ ] ✅ **Chunks** con enemigos configurados
- [ ] ✅ **Enemy Prefab** con EnemyController

---

## 🚀 Setup Automático (RECOMENDADO)

### Opción 1: Usar el Helper Tool

1. En Unity, ve al menú: **Tools > Setup Game Scene**
2. Click en **"⚡ SETUP COMPLETO"**
3. Lee los mensajes de confirmación
4. Configura las referencias faltantes (ver más abajo)

### Opción 2: Manual (si el helper no funciona)

Sigue las secciones a continuación paso a paso.

---

## 📦 1. ScriptableObjects Necesarios

### A) GameConfig

**Ubicación requerida**: `Assets/Resources/GameConfig.asset`

**¿Ya existe?** Sí, ya ha sido movido allí.

**Qué hace**: Mapea los elementos (Fire, Water, etc.) con sus definiciones.

**Cómo verificar**:
```
1. Project > Assets/Resources/GameConfig.asset
2. Inspector > Element Mappings debe tener entries
3. Ejemplo: Fire → FireDefinition.asset
```

**Si falta**: 
```
1. Click derecho en Project > Create > Combate > Game Config
2. Moverlo a Assets/Resources/
3. Arrastrar ElementDefinitions a Element Mappings
```

---

### B) CombatRules

**Ubicación requerida**: `Assets/Resources/CombatRules.asset`

**Qué hace**: Define las reglas de combate (rangos, límites, priorización).

**Cómo crear**:
```
1. Click derecho en Assets/Resources > Create > Combate > Combat Rules
2. Configuración recomendada:
   - Detection Radius: 20
   - Engagement Radius: 10
   - Max Enemies Per Encounter: 5
   - Auto Start Combat: ✅
   - Prioritization: By Distance
```

**O usar el Helper**: Tools > Setup Game Scene > "1. Verificar/Crear CombatRules"

---

## 🏗️ 2. Managers en la Escena

Tu escena necesita estos **5 GameObjects** con sus componentes:

### 1️⃣ WorldChunkManager
```
GameObject: "WorldChunkManager"
Componente: WorldChunkManager

Configurar en Inspector:
✅ Player Transform: Arrastra tu Player aquí
✅ Chunk Size: 256 (⚠️ IMPORTANTE: Valor maestro del sistema)
✅ Load Radius: 2
✅ Update Interval: 1
✅ Show Debug Gizmos: ✅ (para ver los chunks)
```

### 2️⃣ DynamicEnemyPoolManager
```
GameObject: "DynamicEnemyPoolManager"
Componente: DynamicEnemyPoolManager

Configurar en Inspector:
✅ Enemy Controller Prefab: Tu prefab base de enemigo
   (debe tener EnemyController component)
```

### 3️⃣ PlayerPartyManager
```
GameObject: "PlayerPartyManager"
Componente: PlayerPartyManager

Configurar en Inspector:
✅ Max Owned Characters: 20
✅ Max Active Party Size: 5
✅ Max Reinforcement Distance: 100

⚠️ NO necesitas asignar nada más aquí.
El manager se registra automáticamente cuando el player entra en escena.
```

### 4️⃣ CombatEncounterManager
```
GameObject: "CombatEncounterManager"
Componente: CombatEncounterManager

Configurar en Inspector:
✅ Combat Rules: Arrastra CombatRules.asset
✅ Use Player Party Manager: ✅ (checked)

⚠️ NO llenes Manual Party Members si usas PlayerPartyManager.
```

### 5️⃣ CombateManager
```
GameObject: "CombateManager"
Componente: CombateManager

Configurar en Inspector:
✅ Use Legacy Mode: ❌ (unchecked)
✅ Use Player UI Input: ✅ (checked)

⚠️ NO llenes las referencias manuales si usas el modo dinámico.
```

---

## 🎮 3. Configuración del Player

### Componentes Requeridos en el Player

Tu GameObject del player (ej: "Caballero") debe tener:

#### A) EntityController
```
Componente: EntityController

✅ Clase Data: Asigna tu ClaseData (ej: CaballeroData)
✅ Nivel Inicial: 1 (o el que quieras)
```

#### B) EntityStats
```
Componente: EntityStats
(Se auto-configura al inicializar con EntityController)
```

#### C) PlayerInterestZone (opcional pero recomendado)

**Opción 1 - Hijo del Player**:
```
1. Click derecho en Player > Create Empty
2. Nombre: "InterestZone"
3. Add Component > PlayerInterestZone
4. Add Component > Sphere Collider
5. Configurar Sphere Collider:
   - Is Trigger: ✅
   - Radius: 10 (engagement)
   
6. Configurar PlayerInterestZone:
   - Combat Rules: CombatRules.asset
   - Follow Main Character: ✅
   - Use Trigger Mode: ✅
```

**Opción 2 - GameObject Independiente**:
```
1. Hierarchy > Create Empty > "PlayerInterestZone"
2. Add Component > PlayerInterestZone
3. Configurar:
   - Follow Main Character: ✅
   - Player Transform: (se auto-asigna del PartyManager)
```

---

## 🐉 4. Configuración de Enemigos

### A) Crear EnemigoData (ScriptableObject)

```
1. Click derecho > Create > Combate > Enemigo Data
2. Configurar:
   - Nombre: "Goblin"
   - Tipo Enemigo: Goblin
   - Nivel Base: 1
   - Vida Base: 50
   - Ataque Base: 8
   - Defensa Base: 3
   - Velocidad Base: 12
   - XP Otorgada: 25
   - Estilo Combate: Melee
   - Habilidad Por Defecto: AtaqueBasico.asset
```

### B) Crear Prefab de Enemigo

```
1. Hierarchy > Create Empty > "EnemyBase"
2. Add Component > EnemyController
3. Configurar:
   - Datos Enemigo: GoblinData (el SO que creaste)
   
4. Agregar modelo visual (opcional):
   - Arrastra tu modelo 3D como hijo
   - O agrega un Capsule para testing
   
5. Project > Drag "EnemyBase" to create Prefab
6. Asigna este prefab al DynamicEnemyPoolManager
```

---

## 🗺️ 5. Configuración de Chunks

### Opción A: Usar ChunkDataAsset (Recomendado)

```
1. Click derecho > Create > World > Chunk Data
2. Nombre: "Chunk_Test_00"
3. Configurar:
   - Coordinates: (0, 0)
   - Gizmo Color: Verde
   
4. Agregar Enemigo:
   - Click "+" en Enemy Spawns
   - Enemy Data: GoblinData
   - Spawn Position: (10, 0, 10)
   - Initial AI State: Idle
   - Detection Radius: 15
   - Chase Radius: 20
```

### Opción B: Pintar Enemigos en la Escena (Lo que hiciste)

Si ya tienes enemigos pintados en el Scene View con waypoints:

```
1. Selecciona tu ChunkDataAsset
2. En Scene View verás los spawns pintados
3. Asegúrate de que cada spawn tenga:
   ✅ Enemy Data asignado
   ✅ Spawn Position configurada
   ✅ Waypoints configurados (si aplica)
```

---

## 🎯 6. Registro del Player

Cuando la escena inicie, el player debe registrarse en el PlayerPartyManager.

### Opción A: Usar un Script de Inicio

Crea este script en tu Player o en un GameObject de la escena:

```csharp
using UnityEngine;
using Managers;

public class PlayerInitializer : MonoBehaviour
{
    [SerializeField] private EntityController playerController;
    
    void Start()
    {
        if (playerController == null)
        {
            playerController = GetComponent<EntityController>();
        }
        
        if (playerController != null)
        {
            // Registrar como main character
            PlayerPartyManager.Instance.SetMainCharacter(playerController);
            Debug.Log($"✅ Player {playerController.name} registrado como Main");
        }
        else
        {
            Debug.LogError("❌ PlayerController no encontrado");
        }
    }
}
```

### Opción B: Registrar Manualmente desde Otro Script

En cualquier script de inicialización de escena:

```csharp
void Start()
{
    var player = GameObject.Find("Caballero").GetComponent<EntityController>();
    PlayerPartyManager.Instance.SetMainCharacter(player);
}
```

---

## ▶️ 7. Testing - Lo que Debes Ver

### Al darle Play:

1. **Console debe mostrar**:
   ```
   ✅ GameConfig cargado correctamente.
   ✅ PlayerPartyManager: Main character establecido: Caballero
   ✅ WorldChunkManager inicializado
   ```

2. **Scene View debe mostrar**:
   - Gizmos de chunks (cuadrados verdes)
   - Esfera de detección alrededor del player
   - Enemigos spawneados si el player está cerca

3. **Al acercarte a un enemigo**:
   - El enemigo debe detectarte
   - Console: "EventoCandidatoDetectado"
   - Si entras en rango de combate: "Iniciando combate..."

4. **En combate**:
   - Console: "Turno de: [Nombre]"
   - UI mostrará opciones (si está implementada)
   - Enemigos/aliados atacarán por turnos

---

## ❌ Errores Comunes y Soluciones

### Error: "GameConfig no encontrado"
**Causa**: GameConfig no está en Resources/  
**Solución**: 
```
1. Buscar GameConfig.asset en el proyecto
2. Moverlo a Assets/Resources/
3. O usar Tools > Setup Game Scene > "2. Verificar GameConfig"
```

---

### Error: "CombatRules no encontrado"
**Causa**: Falta CombatRules.asset  
**Solución**:
```
1. Tools > Setup Game Scene > "1. Verificar/Crear CombatRules"
2. O crear manualmente: Create > Combate > Combat Rules
```

---

### Error: "NullReferenceException en PlayerPartyManager"
**Causa**: Player no está registrado como Main  
**Solución**:
```
1. Agregar PlayerInitializer script al player
2. O llamar manualmente SetMainCharacter() en Start
```

---

### Error: "No se detectan enemigos"
**Causa**: Multiple posibles  
**Checklist**:
- [ ] PlayerInterestZone tiene collider con Is Trigger ✅
- [ ] El layer del enemigo está en CombatRules.enemyLayers
- [ ] Detection Radius en CombatRules > 0
- [ ] Enemigos tienen EnemyController y implementan ICombatCandidate

---

### Enemigos no spawnean
**Causa**: WorldChunkManager no cargó el chunk  
**Solución**:
```
1. Verificar que el chunk tiene coordinates correctas
2. Player está dentro del Load Radius
3. Console debe mostrar: "Chunk (X,Y) cargado"
4. Usar Show Debug Gizmos para ver chunks
```

---

## 🎨 Workflow de Testing Recomendado

1. **Primera vez** (Setup):
   - Tools > Setup Game Scene > Setup Completo
   - Configurar referencias manualmente
   - Crear 1 enemigo básico

2. **Testing Básico**:
   - Play
   - Mover player cerca del enemigo
   - Verificar detección en Console

3. **Testing Combate** (cuando UI esté lista):
   - Entrar en rango de combate
   - Observar turnos en Console
   - Probar habilidades

4. **Testing Chunks**:
   - Crear múltiples chunks
   - Mover player entre chunks
   - Observar spawning/despawning

---

## 📚 Documentación Relacionada

- [00_GUIA_UNITY.md](00_GUIA_UNITY.md) - Setup general de Unity
- [17_Sistema_Combate.md](17_Sistema_Combate.md) - Detalles del sistema de combate
- [25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md) - Sistema de chunks
- [GUIA_CHUNK_INTEGRATION.md](GUIA_CHUNK_INTEGRATION.md) - Integración de chunks
- [INDICE_ENEMIGOS_CHUNKS.md](INDICE_ENEMIGOS_CHUNKS.md) - Índice completo

---

## 🆘 Soporte

Si sigues teniendo problemas:

1. Verifica la **Console** para mensajes de error específicos
2. Revisa que todos los **ScriptableObjects** estén asignados
3. Usa **Debug Gizmos** para visualizar rangos y chunks
4. Verifica que los **Layers** estén configurados correctamente

---

**Última actualización**: Febrero 2026  
**Versión del sistema**: Post-refactor Chunks + Pooling + Combate Dinámico
