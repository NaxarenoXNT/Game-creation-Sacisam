using UnityEngine;

namespace Missions.Objectives
{
    /// <summary>
    /// Objetivo: recolectar/entregar X cantidad de un item específico.
    /// </summary>
    [CreateAssetMenu(fileName = "MissObj_Item", menuName = "Missions/Objectives/Recolectar Item")]
    public class CollectItemObjectiveSO : MissionObjectiveSO
    {
        [Header("Configuración")]
        [Tooltip("ID del item a recolectar")]
        public string itemId;

        [Tooltip("Nombre del item para UI")]
        public string itemNombre;

        [Tooltip("Cantidad requerida")]
        public int cantidad = 1;

        public override string GetDescripcionAuto()
        {
            string nombre = !string.IsNullOrEmpty(itemNombre) ? itemNombre : itemId;
            return cantidad > 1 ? $"Recolecta {cantidad} {nombre}" : $"Obtén {nombre}";
        }

        public override MissionObjectiveInstance CrearInstancia()
        {
            return new CollectItemObjectiveInstance
            {
                itemId = itemId,
                itemNombre = itemNombre,
                cantidadRequerida = cantidad
            };
        }
    }

    [System.Serializable]
    public class CollectItemObjectiveInstance : MissionObjectiveInstance
    {
        public string itemId;
        public string itemNombre;
        public int cantidadRequerida;
        public int cantidadActual;

        /// <summary>
        /// Registra items obtenidos. Retorna true si el objetivo acaba de completarse.
        /// </summary>
        public bool RegistrarItem(string id, int cantidad = 1)
        {
            if (completado) return false;
            if (id != itemId) return false;

            cantidadActual += cantidad;
            if (cantidadActual >= cantidadRequerida)
            {
                completado = true;
                return true;
            }
            return false;
        }

        public override float GetProgreso()
        {
            return cantidadRequerida > 0 ? Mathf.Clamp01((float)cantidadActual / cantidadRequerida) : 1f;
        }

        public override string GetDescripcionProgreso()
        {
            string nombre = !string.IsNullOrEmpty(itemNombre) ? itemNombre : itemId;
            return $"{cantidadActual}/{cantidadRequerida} {nombre}";
        }
    }
}
