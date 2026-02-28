using Padres;
using System.Collections.Generic;

namespace Habilidades
{
    /// <summary>
    /// Interfaz para efectos de habilidades pasivas.
    /// A diferencia de IHabilidadEffect, estos se aplican/remueven
    /// y permanecen activos mientras la entidad posea la pasiva.
    /// </summary>
    public interface IPasivaEffect
    {
        /// <summary>
        /// Se llama cuando la pasiva se activa (al obtenerla, al inicio del combate, etc.).
        /// </summary>
        void Aplicar(Entidad portador);

        /// <summary>
        /// Se llama cuando la pasiva se desactiva (al perderla, al fin del combate, etc.).
        /// </summary>
        void Remover(Entidad portador);

        /// <summary>
        /// Se llama cada turno mientras la pasiva esté activa.
        /// Útil para regeneración, efectos periódicos, etc.
        /// </summary>
        void ProcesarTurno(Entidad portador);

        /// <summary>
        /// Descripción del efecto para mostrar en UI.
        /// </summary>
        string ObtenerDescripcion();
    }
}
