using System;
using System.Collections.Generic;
using Missions.Objectives;

namespace Missions
{
    /// <summary>
    /// Estado runtime de una misión activa.
    /// Contiene las instancias mutables de los objetivos y el estado actual.
    /// Se crea cuando el jugador acepta una misión y se destruye al completarse/fallar.
    /// </summary>
    [Serializable]
    public class MissionInstance
    {
        /// <summary>Definición inmutable de la misión.</summary>
        public MissionDefinitionSO definition;

        /// <summary>Estado actual de la misión.</summary>
        public MissionStatus status;

        /// <summary>Instancias runtime de cada objetivo (progreso mutable).</summary>
        public List<MissionObjectiveInstance> objetivos;

        /// <summary>Timestamp de cuándo se aceptó la misión.</summary>
        public float tiempoAceptada;

        public MissionInstance(MissionDefinitionSO def)
        {
            definition = def;
            status = MissionStatus.Active;
            tiempoAceptada = UnityEngine.Time.time;

            objetivos = new List<MissionObjectiveInstance>(def.objetivos.Count);
            foreach (var objSO in def.objetivos)
            {
                if (objSO != null)
                    objetivos.Add(objSO.CrearInstancia());
            }
        }

        /// <summary>
        /// Verifica si todos los objetivos obligatorios se completaron.
        /// </summary>
        public bool TodosObjetivosObligatoriosCompletos()
        {
            for (int i = 0; i < objetivos.Count; i++)
            {
                // Verificar si el objetivo original es opcional
                bool esOpcional = i < definition.objetivos.Count
                    && definition.objetivos[i] != null
                    && definition.objetivos[i].opcional;

                if (!esOpcional && !objetivos[i].completado)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Progreso normalizado de la misión (solo cuenta obligatorios).
        /// </summary>
        public float GetProgresoTotal()
        {
            float total = 0f;
            int count = 0;

            for (int i = 0; i < objetivos.Count; i++)
            {
                bool esOpcional = i < definition.objetivos.Count
                    && definition.objetivos[i] != null
                    && definition.objetivos[i].opcional;

                if (!esOpcional)
                {
                    total += objetivos[i].GetProgreso();
                    count++;
                }
            }

            return count > 0 ? total / count : 1f;
        }

        /// <summary>
        /// Obtiene el objetivo runtime en un índice específico.
        /// </summary>
        public T GetObjetivo<T>(int index) where T : MissionObjectiveInstance
        {
            if (index >= 0 && index < objetivos.Count)
                return objetivos[index] as T;
            return null;
        }
    }
}
