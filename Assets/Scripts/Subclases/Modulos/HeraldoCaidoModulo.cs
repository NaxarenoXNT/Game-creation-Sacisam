using Flags;
using Interfaces;
using Padres;
using UnityEngine;

namespace Subclases.Modulos
{
    /// <summary>
    /// Módulo de comportamiento del Heraldo Caído (evolución tier 2 del Paladín).
    ///
    /// Este módulo se apila SOBRE el PaladinModulo, que permanece activo.
    /// Al iterar en reversa (más reciente primero), este módulo gana en los
    /// hooks sustitutivos y el Paladín lo hace en los aditivos.
    ///
    /// Mecánicas diferenciadas vs Paladín:
    ///   - Sustituye el elemento de ataque → Dark en todos los ataques,
    ///     pisando el override Light del Paladín.
    ///   - Las mecánicas aditivas del Paladín (+20% curación, +20% vs Undead)
    ///     se mantienen sin cambios ya que este módulo no las interviene.
    ///   - TODO: completar con habilidades propias al implementar el árbol completo.
    /// </summary>
    public class HeraldoCaidoModulo : IComportamientoDeClase
    {
        public string ModuloId => "heraldo_caido";

        // ── Ciclo de vida ────────────────────────────────────────────────────

        public void AlAgregar(Jugador jugador)
        {
            // TODO: agregar habilidades específicas del Heraldo Caído
            Debug.Log($"[HeraldoCaidoModulo] Módulo agregado a {jugador.Nombre_Entidad}.");
        }

        public void AlRemover(Jugador jugador)
        {
            // TODO: limpiar habilidades específicas del Heraldo Caído
            Debug.Log($"[HeraldoCaidoModulo] Módulo removido de {jugador.Nombre_Entidad}.");
        }

        // ── Hooks ADITIVOS (sin intervención, delega al PaladinModulo) ────────

        public int ModificarCuracionOtorgada(int cantidadBase, IEntidadCombate objetivo)
            => cantidadBase;

        public int ModificarCuracionRecibida(int cantidadBase)
            => cantidadBase;

        // ── Hooks SUSTITUTIVOS ───────────────────────────────────────────────

        /// <summary>
        /// Siempre retorna Dark, reemplazando el Light del Paladín.
        /// Al ser más reciente en la lista _modulos, gana en la iteración reversa.
        /// </summary>
        public ElementAttribute? ModificarElementoAtaque(ElementAttribute elementoBase)
            => ElementAttribute.Dark;

        public TipoRecurso? OverridearRecursoPrincipal() => null;
    }
}
