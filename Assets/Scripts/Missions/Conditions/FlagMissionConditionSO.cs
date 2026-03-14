using UnityEngine;
using Evolution;

namespace Missions.Conditions
{
    /// <summary>
    /// Condición de misión: evalúa un flag personalizado del diseñador.
    /// Permite condiciones arbitrarias como "union_iglesia >= 1".
    /// </summary>
    [CreateAssetMenu(fileName = "MissCond_Flag", menuName = "Missions/Conditions/Custom Flag")]
    public class FlagMissionConditionSO : MissionConditionSO
    {
        public enum Comparador
        {
            Igual,
            Mayor,
            MayorOIgual,
            Menor,
            MenorOIgual,
            Diferente
        }

        [Header("Configuración")]
        [Tooltip("Clave del flag personalizado")]
        public string flagKey;

        [Tooltip("Valor a comparar")]
        public int valorObjetivo;

        [Tooltip("Tipo de comparación")]
        public Comparador comparador = Comparador.MayorOIgual;

        public override bool Evaluar(EvolutionState state)
        {
            if (string.IsNullOrEmpty(flagKey)) return false;
            state.customFlags.TryGetValue(flagKey, out int valorActual);

            return comparador switch
            {
                Comparador.Igual => valorActual == valorObjetivo,
                Comparador.Mayor => valorActual > valorObjetivo,
                Comparador.MayorOIgual => valorActual >= valorObjetivo,
                Comparador.Menor => valorActual < valorObjetivo,
                Comparador.MenorOIgual => valorActual <= valorObjetivo,
                Comparador.Diferente => valorActual != valorObjetivo,
                _ => false
            };
        }

        public override float GetProgreso(EvolutionState state)
        {
            return Evaluar(state) ? 1f : 0f;
        }

        public override string GetDescripcionAuto()
        {
            string op = comparador switch
            {
                Comparador.Igual => "=",
                Comparador.Mayor => ">",
                Comparador.MayorOIgual => ">=",
                Comparador.Menor => "<",
                Comparador.MenorOIgual => "<=",
                Comparador.Diferente => "!=",
                _ => "?"
            };
            return $"{flagKey} {op} {valorObjetivo}";
        }
    }
}
