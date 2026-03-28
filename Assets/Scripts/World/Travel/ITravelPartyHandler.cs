using UnityEngine;

namespace World.Travel
{
    /// <summary>
    /// Contrato para el sistema que reposiciona al party alrededor del
    /// personaje principal tras un viaje rápido.
    ///
    /// Reemplaza la implementación concreta de TravelPartyHandler si el
    /// proyecto necesita una estrategia de formación diferente (ej: seguir
    /// waypoints específicos por personaje, formación en columna, etc.).
    /// </summary>
    public interface ITravelPartyHandler
    {
        /// <summary>
        /// Reposiciona los miembros activos del party alrededor de <paramref name="mainPosition"/>.
        /// Solo afecta al party activo (los estacionados NO se mueven).
        /// El main character ya debe estar en <paramref name="mainPosition"/> antes de llamar esto.
        /// </summary>
        void RepositionParty(Vector3 mainPosition);
    }
}
