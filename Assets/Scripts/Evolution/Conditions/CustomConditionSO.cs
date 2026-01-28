using UnityEngine;

namespace Evolution.Conditions
{
    /// <summary>
    /// Condición genérica para flags custom.
    /// Útil para condiciones especiales o eventos únicos.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_Custom", menuName = "Evolutions/Conditions/Custom Flag")]
    public class CustomConditionSO : EvolutionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("Key del flag custom")]
        public string flagKey;
        
        [Tooltip("Valor mínimo requerido")]
        public int valorMinimo = 1;

        public override bool EsEscalable => valorMinimo > 1;

        public override bool Evaluar(EvolutionState state)
        {
            if (string.IsNullOrEmpty(flagKey)) return false;
            state.customFlags.TryGetValue(flagKey, out int valor);
            return valor >= valorMinimo;
        }

        public override float GetProgreso(EvolutionState state)
        {
            if (string.IsNullOrEmpty(flagKey) || valorMinimo <= 0) 
                return Evaluar(state) ? 1f : 0f;
            
            state.customFlags.TryGetValue(flagKey, out int valor);
            return Mathf.Clamp01((float)valor / valorMinimo);
        }

        public override string GetDescripcionAuto()
        {
            if (valorMinimo <= 1)
                return $"Flag: {flagKey}";
            return $"{flagKey} ≥ {valorMinimo}";
        }

        public override EvolutionConditionSO CrearCopiaEscalada(float multiplicador)
        {
            if (valorMinimo <= 1) return this;
            
            var copia = CreateInstance<CustomConditionSO>();
            copia.flagKey = flagKey;
            copia.valorMinimo = Mathf.RoundToInt(valorMinimo * multiplicador);
            copia.descripcionUI = descripcionUI;
            copia.icono = icono;
            return copia;
        }
    }
}
