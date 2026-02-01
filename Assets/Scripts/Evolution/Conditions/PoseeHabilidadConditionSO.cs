using UnityEngine;

namespace Evolution
{
    /// <summary>
    /// Condición que evalúa si el jugador posee una habilidad específica.
    /// Útil para requisitos previos de evoluciones.
    /// </summary>
    [CreateAssetMenu(fileName = "Nueva Condicion Posee Habilidad", 
        menuName = "Evolution/Conditions/Posee Habilidad")]
    public class PoseeHabilidadConditionSO : EvolutionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("ID de la habilidad requerida")]
        public string habilidadId;
        
        [Tooltip("Referencia opcional al SO de la habilidad")]
        public HabilidadData habilidadReferencia;
        
        [Tooltip("Si es true, la condición se cumple si NO posee la habilidad")]
        public bool invertir;

        private string GetHabilidadId()
        {
            if (habilidadReferencia != null)
                return habilidadReferencia.name;
            return habilidadId;
        }

        public override bool Evaluar(EvolutionState state)
        {
            string id = GetHabilidadId();
            if (string.IsNullOrEmpty(id)) return false;
            
            bool posee = state.TieneHabilidad(id);
            return invertir ? !posee : posee;
        }

        public override float GetProgreso(EvolutionState state)
        {
            // Binario: 0 o 1
            return Evaluar(state) ? 1f : 0f;
        }

        public override string GetDescripcionAuto()
        {
            string nombreHabilidad = habilidadReferencia != null 
                ? habilidadReferencia.nombreHabilidad 
                : habilidadId;
                
            if (invertir)
                return $"No poseer {nombreHabilidad}";
            return $"Poseer {nombreHabilidad}";
        }
    }
}
