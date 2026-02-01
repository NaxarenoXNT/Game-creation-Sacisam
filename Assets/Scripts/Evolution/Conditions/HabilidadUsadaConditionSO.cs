using UnityEngine;

namespace Evolution
{
    /// <summary>
    /// Condición que evalúa si una habilidad específica ha sido usada X veces.
    /// Útil para evoluciones basadas en dominio de habilidades.
    /// </summary>
    [CreateAssetMenu(fileName = "Nueva Condicion Habilidad Usada", 
        menuName = "Evolution/Conditions/Habilidad Usada")]
    public class HabilidadUsadaConditionSO : EvolutionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("ID de la habilidad a trackear")]
        public string habilidadId;
        
        [Tooltip("Referencia opcional al SO de la habilidad")]
        public HabilidadData habilidadReferencia;
        
        [Tooltip("Cantidad de usos requeridos")]
        [Min(1)]
        public int cantidadRequerida = 10;

        public override bool EsEscalable => true;

        private string GetHabilidadId()
        {
            // Priorizar referencia directa si existe
            if (habilidadReferencia != null)
                return habilidadReferencia.name;
            return habilidadId;
        }

        public override bool Evaluar(EvolutionState state)
        {
            string id = GetHabilidadId();
            if (string.IsNullOrEmpty(id)) return false;
            
            return state.GetUsosHabilidad(id) >= cantidadRequerida;
        }

        public override float GetProgreso(EvolutionState state)
        {
            string id = GetHabilidadId();
            if (string.IsNullOrEmpty(id) || cantidadRequerida <= 0) return 0f;
            
            int usos = state.GetUsosHabilidad(id);
            return Mathf.Clamp01((float)usos / cantidadRequerida);
        }

        public override string GetDescripcionAuto()
        {
            string nombreHabilidad = habilidadReferencia != null 
                ? habilidadReferencia.nombreHabilidad 
                : habilidadId;
            return $"Usar {nombreHabilidad} {cantidadRequerida} veces";
        }

        public override EvolutionConditionSO CrearCopiaEscalada(float multiplicador)
        {
            var copia = Instantiate(this);
            copia.cantidadRequerida = Mathf.RoundToInt(cantidadRequerida * multiplicador);
            copia.name = $"{name}_x{multiplicador:F1}";
            return copia;
        }
    }
}
