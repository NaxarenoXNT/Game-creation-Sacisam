using System;
using System.Collections.Generic;
using GameInput;
using UI.Combat;
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

            // El GameFlow es el único responsable de mostrar/ocultar el HUD.
            // HUDController actualiza datos internamente via EventoCombateIniciado.
            // EnsureInstance() crea los componentes si no existen en la escena.
            var uiCtrl = CombatUIController.EnsureInstance();
            if (uiCtrl != null)
            {
                uiCtrl.MostrarHUD();
            }
            else
            {
                Debug.LogError("[CombatFlow] No se pudo crear CombatUIController. " +
                               "Verifica que exista un UIDocument con HUD.uxml en la escena.");
            }
        }

        public void Exit()
        {
            Debug.Log("[CombatFlow] ← Exit: Desactivando modo combate");

            // Ocultar toda la UI de combate. El input lo restaura el siguiente estado.
            CombatUIController.Instance?.OcultarHUD();
        }
    }
}
