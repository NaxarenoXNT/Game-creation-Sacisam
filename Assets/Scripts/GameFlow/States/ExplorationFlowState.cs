using System;
using System.Collections.Generic;
using GameInput;
using UnityEngine;

namespace GameFlow
{
    /// <summary>
    /// Estado de exploración del mundo.
    /// Activa movimiento WASD, input de exploración y sistemas del mundo abierto.
    /// Es el estado base/por defecto del juego.
    /// </summary>
    public class ExplorationFlowState : IGameFlowState
    {
        public bool BlocksLowerStates => true;

        public IEnumerable<Type> AllowedTransitions => new[]
        {
            typeof(CombatFlowState),
            // Futuros estados:
            // typeof(InventoryFlowState),
            // typeof(DialogueFlowState),
            // typeof(PauseFlowState),
            // typeof(ShopFlowState),
        };

        public void Enter()
        {
            Debug.Log("[ExplorationFlow] → Enter: Activando exploración");

            // Configurar input para exploración
            if (GameInputManager.Instance != null)
            {
                GameInputManager.Instance.SetContext(InputContext.Exploration);
            }

            // Aquí se podrían activar sistemas de exploración:
            // - Habilitar movimiento del jugador
            // - Mostrar UI de exploración (minimap, etc.)
            // - Reanudar sistemas del mundo (IA de NPCs, clima, etc.)
        }

        public void Exit()
        {
            Debug.Log("[ExplorationFlow] ← Exit: Desactivando exploración");

            // Aquí se podrían desactivar sistemas de exploración:
            // - El input se cambiará por el próximo estado
            // - Ocultar UI de exploración si es necesario
        }
    }
}
