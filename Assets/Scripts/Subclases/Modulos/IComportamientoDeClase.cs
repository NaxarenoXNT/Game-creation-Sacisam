using Flags;
using Interfaces;
using Padres;

namespace Subclases.Modulos
{
    /// <summary>
    /// Contrato para módulos de comportamiento que se inyectan en un Jugador
    /// al obtener una evolución de clase.
    ///
    /// Reglas de consulta:
    /// - Hooks ADITIVOS     : todos los módulos contribuyen (se itera de primero a último).
    /// - Hooks SUSTITUTIVOS : el módulo más reciente que responda con un valor no-null gana
    ///                        (se itera de último a primero).
    ///
    /// Esto permite que evoluciones posteriores "pisen" propiedades de anteriores
    /// (ej: HeraldoCaído reemplaza el Light del Paladín con Dark) mientras que
    /// otros bonos como la curación se van acumulando.
    /// </summary>
    public interface IComportamientoDeClase
    {
        /// <summary>Identificador único del módulo. Evita duplicados al agregar.</summary>
        string ModuloId { get; }

        // ── Ciclo de vida ────────────────────────────────────────────────────

        /// <summary>Ejecutado cuando el módulo se agrega al jugador (al evolucionar).</summary>
        void AlAgregar(Jugador jugador);

        /// <summary>Ejecutado cuando el módulo se remueve del jugador.</summary>
        void AlRemover(Jugador jugador);

        // ── Hooks ADITIVOS ───────────────────────────────────────────────────

        /// <summary>
        /// Modifica la curación que este jugador OTORGA a otros.
        /// Todos los módulos contribuyen en cadena.
        /// </summary>
        int ModificarCuracionOtorgada(int cantidadBase, IEntidadCombate objetivo);

        /// <summary>
        /// Modifica la curación que este jugador RECIBE.
        /// Todos los módulos contribuyen en cadena.
        /// </summary>
        int ModificarCuracionRecibida(int cantidadBase);

        // ── Hooks SUSTITUTIVOS ───────────────────────────────────────────────

        /// <summary>
        /// Override del elemento de ataque.
        /// Retornar null si este módulo no interviene en el elemento.
        /// El módulo más reciente que retorne un valor no-null gana.
        /// </summary>
        ElementAttribute? ModificarElementoAtaque(ElementAttribute elementoBase);

        /// <summary>
        /// Override del recurso principal (ej: Paladín → Fe cuando Fe esté diseñada).
        /// Retornar null si este módulo no cambia el recurso.
        /// El módulo más reciente que retorne un valor no-null gana.
        /// </summary>
        TipoRecurso? OverridearRecursoPrincipal();
    }
}
