using System;
using System.Collections.Generic;
using GameInput;
using UnityEngine;

namespace GameFlow
{
    /// <summary>
    /// Estado activo mientras la ficha de personaje (CharacterSheet) está abierta.
    /// Se superpone a ExplorationFlowState sin bloquearlo (BlocksLowerStates = false).
    /// Permite transición a CombatFlowState para que el combate siempre pueda iniciar.
    /// </summary>
    public class CharacterSheetFlowState : IGameFlowState
    {
        public bool BlocksLowerStates => false;

        public IEnumerable<Type> AllowedTransitions => new[]
        {
            typeof(CombatFlowState),
        };

        public void Enter()
        {
            Debug.Log("[CharacterSheetFlow] → Enter: Input a Menu");
            if (GameInputManager.Instance != null)
                GameInputManager.Instance.SetContext(InputContext.Menu);
        }

        public void Exit()
        {
            Debug.Log("[CharacterSheetFlow] ← Exit: Cerrando ficha de personaje");
            // El estado subyacente restaurará el input correcto via su Enter().
        }
    }
}
