using UnityEngine;

namespace World.ChunkSystem
{
    /// <summary>
    /// Define un tipo de objeto del mundo: edificio, cofre, NPC estático, consumible, etc.
    /// Crear desde: Project → Create → World → Prop Data
    /// </summary>
    [CreateAssetMenu(menuName = "World/Prop Data", fileName = "NewProp")]
    public class PropData : ScriptableObject
    {
        [Header("Información")]
        public string propName = "Nuevo Prop";
        public PropCategory category = PropCategory.Decoration;

        [Header("Visual")]
        [Tooltip("Prefab que se instancia en el mundo.")]
        public GameObject prefab;

        [Header("Interacción")]
        [Tooltip("Si el jugador puede interactuar con este objeto.")]
        public bool isInteractive = false;

        [Tooltip("Si está activo, el objeto desaparece cuando el jugador interactúa con él.")]
        public bool consumeOnInteract = false;

        [Tooltip("Si está activo, el sistema recuerda que este objeto fue consumido incluso " +
                 "después de recargar el chunk. Requiere integración con SaveManager (TODO).")]
        public bool persistConsumedState = false;
    }

    public enum PropCategory
    {
        Decoration,     // Puramente visual, sin interacción
        Structure,      // Edificios, ruinas, muros
        Interactive,    // Cofres, puertas, mecanismos
        Resource,       // Recursos recolectables (si se implementa en el futuro)
        NPC,            // Personajes estáticos
        ZoneEntry       // Entrada a otra zona (cueva, dungeon, etc.)
    }
}