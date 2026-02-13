# Sistema de Misiones y Mundo Vivo

> Documento de diseño y arquitectura para implementar misiones dinámicas, mundo reactivo y narrativa emergente en Unity, alineado con la arquitectura existente del proyecto.

---

## 1. Objetivo del Sistema

Diseñar un sistema de misiones **sin historia principal**, donde:

* Cada zona, ciudad, facción, religión y NPC tenga su propia narrativa.
* El mundo avance con o sin la intervención del jugador.
* Las decisiones tengan consecuencias permanentes.
* La muerte o incapacidad de NPCs afecte misiones, zonas y narrativa.
* Las misiones se adapten al estado del mundo o se pierdan definitivamente.

El foco está en **narrativa sistémica**, no en scripts rígidos.

---

## 2. Principios de Diseño

1. **Data-driven**: las misiones son datos, no lógica procedural.
2. **Separación de responsabilidades**: mundo, misiones, NPCs y consecuencias desacoplados.
3. **Roles antes que identidades**: las misiones dependen de roles, no de NPCs concretos.
4. **Pérdida real de contenido**: no todo es salvable.
5. **Consistencia del mundo**: evitar estados imposibles o contradictorios.
6. **Escalabilidad**: agregar contenido no debe requerir reescribir código.

---

## 3. Conceptos Clave

### 3.1 WorldState

Fuente única de verdad del estado del mundo.

Contiene:

* Estado de NPCs (vivo, muerto, incapacitado)
* Estado de zonas (abierta, sellada, destruida, en evento)
* Flags narrativos globales
* Relaciones entre facciones

**No depende de Unity (clases C# puras).**

```csharp
class WorldState
{
    Dictionary<string, NPCState> npcs;
    Dictionary<string, ZoneState> zonas;
    HashSet<string> flags;
}
```

---

### 3.2 NPC

Un NPC se divide conceptualmente en:

* **Rol funcional** (reemplazable)
* **Arco narrativo personal** (no reemplazable)

Ejemplo:

* Rol: `VendedorPrincipal`
* Arco: `DeudaConElGremio`

Si el NPC muere:

* El rol puede reasignarse.
* El arco narrativo se pierde o falla.

---

### 3.3 Roles (QuestRoleSO)

Los roles representan **funciones narrativas o sistémicas** que una misión necesita.

```csharp
[CreateAssetMenu(menuName = "World/Rol")]
public class QuestRoleSO : ScriptableObject
{
    public string roleId;
    public string descripcion;
}
```

Ejemplos:

* VendedorPrincipal
* SacerdoteCulto
* GobernanteZona

---

## 4. Sistema de Misiones

### 4.1 QuestDefinitionSO

Representa una misión como **definición de datos**.

```csharp
[CreateAssetMenu(menuName = "Quests/Quest Definition")]
public class QuestDefinitionSO : ScriptableObject
{
    public string questId;
    public string nombre;
    public string descripcion;

    public List<QuestConditionSO> condicionesActivacion;
    public List<QuestRequirement> requerimientos;
    public List<QuestVariantSO> variantes;
    public List<QuestConsequenceSO> consecuencias;
}
```

---

### 4.2 Condiciones de Misión (QuestConditionSO)

Determinan **cuándo una misión está disponible**.

Siguen el mismo patrón que `EvolutionConditionSO`.

```csharp
public abstract class QuestConditionSO : ScriptableObject
{
    public abstract bool Evaluar(WorldState world, PlayerState player);
}
```

Ejemplos:

* NPCAliveConditionSO
* ZoneStateConditionSO
* TraitConditionSO
* QuestCompletedConditionSO
* FlagConditionSO

---

### 4.3 Requerimientos de Misión

Definen **qué recursos necesita la misión para ejecutarse**.

```csharp
[Serializable]
public class QuestRequirement
{
    public QuestRoleSO rolRequerido;
    public bool esCritico;
}
```

* `esCritico = true`: si no se puede resolver, la misión se bloquea.
* `esCritico = false`: el rol puede reasignarse.

---

### 4.4 Resolución de Roles (Resource Resolver)

Sistema encargado de:

* Buscar NPCs que cumplan un rol
* Priorizar según criterios:

  * Estado (vivo)
  * Facción
  * Relación
  * Proximidad

```csharp
NPC ResolverRol(QuestRoleSO rol);
```

---

### 4.5 Variantes de Misión (QuestVariantSO)

Permiten que una misión **se adapte al estado del mundo**.

```csharp
[CreateAssetMenu(menuName = "Quests/Quest Variant")]
public class QuestVariantSO : ScriptableObject
{
    public List<QuestConditionSO> condiciones;
    public DialogueSO dialogo;
    public ZoneSO zona;
}
```

El sistema selecciona **la primera variante válida**.

Ejemplos:

* NPC original vivo
* NPC reemplazado
* Zona sellada

---

### 4.6 Consecuencias de Misión (QuestConsequenceSO)

Efectos permanentes sobre el mundo.

```csharp
public abstract class QuestConsequenceSO : ScriptableObject
{
    public abstract void Aplicar(WorldState world);
}
```

Ejemplos:

* Cambiar estado de zona
* Matar o incapacitar NPC
* Activar flags
* Desbloquear nuevas misiones

---

## 5. Arcos Narrativos

### 5.1 NarrativeArc

Representa la historia personal de un NPC.

```csharp
class NarrativeArc
{
    public string arcId;
    public ArcState estado; // Activo, Fallido, Completado
    public List<ArcConsequence> consecuencias;
}
```

Reglas:

* No controla misiones directamente.
* Cambia de estado según eventos del mundo.
* Dispara consecuencias narrativas.

---

## 6. Zonas y Bloqueos

Las zonas pueden tener estados:

* Abierta
* Sellada
* EnEvento
* Destruida

### Reglas:

* Zonas secundarias pueden perderse permanentemente.
* Zonas principales ofrecen rutas alternativas.
* Un bloqueo siempre genera consecuencias nuevas.

---

## 7. QuestManager (Unity)

MonoBehaviour encargado de:

* Evaluar misiones disponibles
* Resolver roles
* Seleccionar variantes
* Aplicar consecuencias

**No contiene lógica narrativa.**

---

## 8. Flujo Completo

1. El mundo cambia (evento, muerte, decisión).
2. WorldState se actualiza.
3. QuestManager reevalúa misiones.
4. Nuevas misiones aparecen o se bloquean.
5. El jugador actúa.
6. Se aplican consecuencias.
7. El mundo evoluciona.

---

## 9. Comparación con Sistema de Traits

| Traits               | Misiones         |
| -------------------- | ---------------- |
| TraitDefinition      | QuestDefinition  |
| EvolutionConditionSO | QuestConditionSO |
| TraitChain           | QuestVariants    |
| EvolutionState       | WorldState       |
| Efectos              | Consecuencias    |

---

## 10. Implementación Recomendada (Roadmap)

1. Crear `QuestConditionSO` base.
2. Implementar `WorldState` mínimo.
3. Crear `QuestDefinitionSO` simple.
4. Implementar `ResourceResolver` básico.
5. Una misión con 2 variantes.
6. Integrar con EventBus.

---

## 11. Objetivo Final

Un mundo que:

* No gira alrededor del jugador.
* No garantiza finales felices.
* Reacciona de forma coherente.
* Genera historias emergentes.

Este sistema prioriza **consistencia, escalabilidad y peso narrativo real**.
