using UnityEngine;
using Flags;

namespace Evolution.Conditions
{
    /// <summary>
    /// Condición: Estado aplicado X veces.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_Estado", menuName = "Evolutions/Conditions/Estado Aplicado")]
    public class EstadoConditionSO : EvolutionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("Estado de combate")]
        public StatusFlag estado = StatusFlag.None;
        
        [Tooltip("Veces que debe haberse aplicado (0 = solo verificar si está activo)")]
        public int vecesAplicado = 0;

        public override bool EsEscalable => vecesAplicado > 0;

        public override bool Evaluar(EvolutionState state)
        {
            string key = estado.ToString();
            
            if (vecesAplicado > 0)
            {
                state.estadosAplicados.TryGetValue(key, out int veces);
                return veces >= vecesAplicado;
            }
            else
            {
                return state.estadosActivos.Contains(key);
            }
        }

        public override float GetProgreso(EvolutionState state)
        {
            if (vecesAplicado <= 0)
                return Evaluar(state) ? 1f : 0f;

            string key = estado.ToString();
            state.estadosAplicados.TryGetValue(key, out int veces);
            return Mathf.Clamp01((float)veces / vecesAplicado);
        }

        public override string GetDescripcionAuto()
        {
            if (vecesAplicado > 0)
                return $"Aplica {estado} {vecesAplicado} veces";
            return $"Tener {estado} activo";
        }

        public override EvolutionConditionSO CrearCopiaEscalada(float multiplicador)
        {
            if (vecesAplicado <= 0) return this;
            
            var copia = CreateInstance<EstadoConditionSO>();
            copia.estado = estado;
            copia.vecesAplicado = Mathf.RoundToInt(vecesAplicado * multiplicador);
            copia.descripcionUI = descripcionUI;
            copia.icono = icono;
            return copia;
        }
    }
}
