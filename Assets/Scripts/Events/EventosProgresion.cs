using Interfaces;

namespace Managers
{
    // =================================================================
    // =================== EVENTOS DE PROGRESION =======================
    // =================================================================
    
    public struct EventoNivelSubido : IEvento
    {
        public IEntidadCombate Entidad;
        public int NuevoNivel;
    }
    
    public struct EventoXPGanada : IEvento
    {
        public IEntidadCombate Entidad;
        public float Cantidad;
        public float Total;
        public float Necesaria;
    }

    /// <summary>
    /// Un personaje obtuvo un trait.
    /// Publicado por EvolutionController tras aplicar un trait.
    /// </summary>
    public struct EventoTraitObtenido : IEvento
    {
        /// <summary>ID del trait obtenido.</summary>
        public string TraitId;

        /// <summary>ID del personaje que obtuvo el trait.</summary>
        public string CharacterId;

        /// <summary>Stacks actuales del trait en ese personaje.</summary>
        public int StacksActuales;

        /// <summary>Si el trait es globalmente único (no re-obtenible por otros).</summary>
        public bool EsGlobalmenteUnico;
    }

    /// <summary>
    /// Un personaje evolucionó de clase.
    /// Publicado por EvolutionController tras aplicar una evolución.
    /// </summary>
    public struct EventoEvolucionAplicada : IEvento
    {
        /// <summary>ID de la evolución aplicada.</summary>
        public string EvolucionId;

        /// <summary>ID del personaje que evolucionó.</summary>
        public string CharacterId;
    }
}
