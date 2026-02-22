using System;
using System.Collections.Generic;
using Padres;

namespace Efectos
{
    /// <summary>
    /// Estado runtime de un efecto activo sobre una entidad concreta.
    /// Una instancia es creada por EffectHandler.AddEffect y destruida cuando expira.
    /// El SO nunca tiene estado mutable — toda la información de estado vive aquí.
    /// </summary>
    public class EffectInstance
    {
        /// <summary>La definición del efecto (asset compartido, inmutable).</summary>
        public EffectDefinitionSO Definition { get; }

        /// <summary>La entidad que porta este efecto.</summary>
        public Entidad Owner { get; }

        /// <summary>La entidad que aplicó el efecto (puede ser null si es ambiental).</summary>
        public Entidad Source { get; }

        /// <summary>Turnos que le quedan activo. Cuando llega a 0 el efecto expira.</summary>
        public int RemainingTurns { get; set; }

        /// <summary>Stacks activos (solo relevante si Definition.stackable == true).</summary>
        public int CurrentStacks { get; set; }

        /// <summary>
        /// Espacio de estado mutable libre para el modificador.
        /// Cada IEffectModifier guarda aquí lo que necesite durante su vida
        /// (ej: sourceAttack capturado en OnApply, contadores internos, etc.)
        /// </summary>
        public Dictionary<string, float> RuntimeState { get; } = new Dictionary<string, float>();

        // -------------------------------------------------------

        public EffectInstance(EffectDefinitionSO definition, Entidad owner, Entidad source)
        {
            Definition     = definition ?? throw new ArgumentNullException(nameof(definition));
            Owner          = owner      ?? throw new ArgumentNullException(nameof(owner));
            Source         = source;
            RemainingTurns = definition.duration;
            CurrentStacks  = 1;
        }

        /// <summary>true cuando el efecto ha agotado todos sus turnos.</summary>
        public bool HasExpired => RemainingTurns <= 0;

        /// <summary>
        /// Shortcut para leer un parámetro del SO directamente desde la instancia.
        /// </summary>
        public float GetParam(string key, float defaultValue = 0f)
            => Definition.GetParam(key, defaultValue);

        public override string ToString()
            => $"{Definition.displayName} [{CurrentStacks}x] ({RemainingTurns}t)";
    }
}
