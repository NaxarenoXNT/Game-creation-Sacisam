using UnityEngine;

namespace CharacterSelection
{
    /// <summary>
    /// Setup de la escena de selección de personaje.
    /// Coloca este script en un GameObject vacío en la escena CharacterSelection.
    /// Asegura que los managers necesarios existan (PlayerPartyManager, etc.)
    /// antes de que la UI comience a funcionar.
    /// </summary>
    public class CharacterSelectionBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // Asegurar que PlayerPartyManager existe (se crea solo si no hay instancia)
            _ = Managers.PlayerPartyManager.Instance;

            Debug.Log("[CharacterSelection] Escena de selección inicializada.");
        }
    }
}
