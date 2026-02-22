using System;
using System.Collections.Generic;

namespace GameFlow
{
    /// <summary>
    /// Contrato para cada estado del flujo global del juego.
    /// Cada estado se autogestiona: configura input, UI, sistemas.
    /// El GameFlowController no conoce detalles internos.
    /// </summary>
    public interface IGameFlowState
    {
        /// <summary>
        /// Se invoca al entrar (o re-entrar tras un Pop superior) en este estado.
        /// Responsable de activar sistemas, configurar input, mostrar UI.
        /// </summary>
        void Enter();

        /// <summary>
        /// Se invoca al salir del estado (por Push de otro o Pop propio).
        /// Responsable de desactivar sistemas, restaurar input, ocultar UI.
        /// </summary>
        void Exit();

        /// <summary>
        /// Si true, al hacer Push de este estado se llama Exit() en el estado inferior.
        /// Ejemplo: Combat bloquea Exploration. Pause bloquea Combat.
        /// Si false, el estado inferior permanece activo (coexistencia parcial).
        /// </summary>
        bool BlocksLowerStates { get; }

        /// <summary>
        /// Lista de tipos de estado a los que se puede transicionar desde este.
        /// Si un estado no aparece en la lista, el Push será rechazado con warning.
        /// </summary>
        IEnumerable<Type> AllowedTransitions { get; }
    }
}
