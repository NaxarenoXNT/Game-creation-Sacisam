using UnityEngine;

namespace Evolution.Conditions
{
    public enum KarmaComparacion
    {
        Minimo,     // karma >= valor
        Maximo,     // karma <= valor
        Rango       // valorMin <= karma <= valorMax
    }

    /// <summary>
    /// Condición: Karma dentro de ciertos parámetros.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_Karma", menuName = "Evolutions/Conditions/Karma")]
    public class KarmaConditionSO : EvolutionConditionSO
    {
        [Header("Configuración")]
        public KarmaComparacion comparacion = KarmaComparacion.Minimo;
        
        [Range(-1f, 1f)]
        [Tooltip("Valor de karma (o mínimo si es rango)")]
        public float valor = 0f;
        
        [Range(-1f, 1f)]
        [Tooltip("Valor máximo (solo para rango)")]
        public float valorMax = 1f;

        public override bool EsEscalable => false;

        public override bool Evaluar(EvolutionState state)
        {
            return comparacion switch
            {
                KarmaComparacion.Minimo => state.karma >= valor,
                KarmaComparacion.Maximo => state.karma <= valor,
                KarmaComparacion.Rango => state.karma >= valor && state.karma <= valorMax,
                _ => false
            };
        }

        public override float GetProgreso(EvolutionState state)
        {
            // Karma no tiene progreso lineal, es binario
            return Evaluar(state) ? 1f : 0f;
        }

        public override string GetDescripcionAuto()
        {
            return comparacion switch
            {
                KarmaComparacion.Minimo => $"Karma ≥ {valor:F1}",
                KarmaComparacion.Maximo => $"Karma ≤ {valor:F1}",
                KarmaComparacion.Rango => $"Karma entre {valor:F1} y {valorMax:F1}",
                _ => "Karma"
            };
        }
    }
}
