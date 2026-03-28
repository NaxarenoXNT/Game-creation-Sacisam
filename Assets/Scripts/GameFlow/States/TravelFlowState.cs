using System;
using System.Collections.Generic;
using GameInput;
using UnityEngine;

namespace GameFlow
{
    /// <summary>
    /// Estado activo mientras el TravelManager ejecuta el pipeline de viaje rápido.
    ///
    /// Responsabilidades:
    ///   - Bloquear todo input del jugador (contexto Menu).
    ///   - Impedir que cualquier otro sistema apile nuevos estados.
    ///   - Devolver el control a ExplorationFlowState vía Pop() cuando el viaje termina.
    ///
    /// Nunca se empuja manualmente desde la UI. Solo TravelManager lo gestiona.
    /// </summary>
    public class TravelFlowState : IGameFlowState
    {
        /// <summary>
        /// Bloquea el estado de exploración mientras el viaje está en curso.
        /// ExplorationFlowState recibe Exit() y lo recupera con Enter() al hacer Pop().
        /// </summary>
        public bool BlocksLowerStates => true;

        /// <summary>
        /// No se permite ninguna transición mientras el viaje está en curso.
        /// Devolver un array vacío (no null) fuerza el rechazo explícito en el FlowController.
        /// </summary>
        public IEnumerable<Type> AllowedTransitions => Array.Empty<Type>();

        public void Enter()
        {
            Debug.Log("[TravelFlow] → Enter: Input bloqueado durante el viaje.");

            if (GameInputManager.Instance != null)
                GameInputManager.Instance.SetContext(InputContext.Travel);
        }

        public void Exit()
        {
            Debug.Log("[TravelFlow] ← Exit: Viaje finalizado, restaurando exploración.");
            // El ExplorationFlowState.Enter() restaurará el contexto Exploration.
        }
    }
}
