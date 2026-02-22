using System;
using System.Collections.Generic;
using GameInput;
using UnityEngine;

namespace GameFlow
{
    /// <summary>
    /// Estado de combate por turnos.
    /// Configura el input para combate, bloquea la exploración.
    /// La lógica de combate ya vive en CombateManager/TurnManager;
    /// este estado solo orquesta la transición del modo global.
    /// </summary>
    public class CombatFlowState : IGameFlowState
    {
        public bool BlocksLowerStates => true;

        public IEnumerable<Type> AllowedTransitions => new[]
        {
            typeof(ExplorationFlowState),
            // Futuros estados superpuestos al combate:
            // typeof(PauseFlowState),
            // typeof(InventoryFlowState),
        };

        public void Enter()
        {
            Debug.Log("[CombatFlow] → Enter: Activando modo combate");

            // Configurar input para combate
            if (GameInputManager.Instance != null)
            {
                GameInputManager.Instance.SetContext(InputContext.Combat);
            }

            // Aquí se podrían activar sistemas de combate:
            // - La UI de combate ya reacciona a EventoCombateIniciado (via HUDController)
            // - Deshabilitar sistemas del mundo que no aplican en combate
        }

        public void Exit()
        {
            Debug.Log("[CombatFlow] ← Exit: Desactivando modo combate");

            // Aquí se podrían limpiar sistemas de combate:
            // - La UI de combate ya reacciona a EventoCombateFinalizado
            // - El input se restaurará por el estado que reciba Enter()
        }
    }
}
