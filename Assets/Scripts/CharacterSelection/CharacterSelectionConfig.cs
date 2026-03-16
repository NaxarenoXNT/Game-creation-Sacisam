using System.Collections.Generic;
using UnityEngine;

namespace CharacterSelection
{
    /// <summary>
    /// Configuración de la pantalla de selección de personaje.
    /// Define las clases disponibles y el prefab base para instanciar personajes.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterSelectionConfig", menuName = "Saclisam/Character Selection Config")]
    public class CharacterSelectionConfig : ScriptableObject
    {
        [Header("Clases Disponibles")]
        [Tooltip("Lista de clases que el jugador puede seleccionar al crear un personaje")]
        public List<ClaseData> clasesDisponibles = new List<ClaseData>();

        [Header("Prefab")]
        [Tooltip("Prefab base del jugador. Debe tener EntityController + EntityStats")]
        public GameObject playerPrefab;

        [Header("Party")]
        [Tooltip("Máximo de personajes que se pueden crear antes de empezar")]
        public int maxPersonajesInicial = 1;

        [Tooltip("Mínimo de personajes requeridos para iniciar")]
        public int minPersonajesRequeridos = 1;

        [Header("Escena Destino")]
        [Tooltip("Nombre de la escena de gameplay a cargar tras seleccionar")]
        public string escenaDestino = "Mundo";
    }
}
