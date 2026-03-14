using UnityEngine;
using Flags;

namespace Missions.Objectives
{
    /// <summary>
    /// Objetivo: eliminar X enemigos de un tipo específico.
    /// </summary>
    [CreateAssetMenu(fileName = "MissObj_Kills", menuName = "Missions/Objectives/Kills Tipo")]
    public class KillObjectiveSO : MissionObjectiveSO
    {
        [Header("Configuración")]
        [Tooltip("Tipo de entidad a eliminar")]
        public TipoEntidades tipoEntidad = TipoEntidades.None;

        [Tooltip("Cantidad requerida")]
        public int cantidad = 10;

        public override string GetDescripcionAuto()
        {
            return $"Elimina {cantidad} {tipoEntidad}";
        }

        public override MissionObjectiveInstance CrearInstancia()
        {
            return new KillObjectiveInstance
            {
                tipoEntidad = tipoEntidad,
                cantidadRequerida = cantidad
            };
        }
    }

    [System.Serializable]
    public class KillObjectiveInstance : MissionObjectiveInstance
    {
        public TipoEntidades tipoEntidad;
        public int cantidadRequerida;
        public int cantidadActual;

        /// <summary>
        /// Registra un kill. Retorna true si el objetivo acaba de completarse.
        /// </summary>
        public bool RegistrarKill(TipoEntidades tipo)
        {
            if (completado) return false;
            if (tipoEntidad != TipoEntidades.None && tipo != tipoEntidad) return false;

            cantidadActual++;
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
            return $"{cantidadActual}/{cantidadRequerida} {tipoEntidad}";
        }
    }
}
