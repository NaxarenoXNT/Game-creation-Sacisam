using UnityEngine;

namespace Missions.Objectives
{
    /// <summary>
    /// Objetivo: llegar a una zona/ubicación/bioma específico.
    /// </summary>
    [CreateAssetMenu(fileName = "MissObj_Zona", menuName = "Missions/Objectives/Llegar a Zona")]
    public class ReachZoneObjectiveSO : MissionObjectiveSO
    {
        [Header("Configuración")]
        [Tooltip("ID de la zona/bioma destino")]
        public string zonaId;

        [Tooltip("Nombre de la zona para UI")]
        public string zonaNombre;

        public override string GetDescripcionAuto()
        {
            string nombre = !string.IsNullOrEmpty(zonaNombre) ? zonaNombre : zonaId;
            return $"Viaja a {nombre}";
        }

        public override MissionObjectiveInstance CrearInstancia()
        {
            return new ReachZoneObjectiveInstance
            {
                zonaId = zonaId,
                zonaNombre = zonaNombre
            };
        }
    }

    [System.Serializable]
    public class ReachZoneObjectiveInstance : MissionObjectiveInstance
    {
        public string zonaId;
        public string zonaNombre;

        /// <summary>
        /// Registra la llegada a una zona. Retorna true si es la zona objetivo.
        /// </summary>
        public bool RegistrarZona(string id)
        {
            if (completado) return false;
            if (id != zonaId) return false;

            completado = true;
            return true;
        }

        public override float GetProgreso()
        {
            return completado ? 1f : 0f;
        }

        public override string GetDescripcionProgreso()
        {
            string nombre = !string.IsNullOrEmpty(zonaNombre) ? zonaNombre : zonaId;
            return completado ? $"Llegaste a {nombre}" : $"Viaja a {nombre}";
        }
    }
}
