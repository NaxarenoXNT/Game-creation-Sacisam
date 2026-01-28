using UnityEngine;

namespace Evolution.Conditions
{
    /// <summary>
    /// Condición: Total de kills de cualquier tipo.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_KillsTotal", menuName = "Evolutions/Conditions/Kills Total")]
    public class KillsTotalConditionSO : EvolutionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("Cantidad total de kills requeridos")]
        public int cantidad = 100;

        public override bool EsEscalable => true;

        public override bool Evaluar(EvolutionState state)
        {
            int total = 0;
            foreach (var kvp in state.killsPorTipo)
                total += kvp.Value;
            return total >= cantidad;
        }

        public override float GetProgreso(EvolutionState state)
        {
            int total = 0;
            foreach (var kvp in state.killsPorTipo)
                total += kvp.Value;
            return cantidad > 0 ? Mathf.Clamp01((float)total / cantidad) : 1f;
        }

        public override string GetDescripcionAuto()
        {
            return $"Elimina {cantidad} enemigos";
        }

        public override EvolutionConditionSO CrearCopiaEscalada(float multiplicador)
        {
            var copia = CreateInstance<KillsTotalConditionSO>();
            copia.cantidad = Mathf.RoundToInt(cantidad * multiplicador);
            copia.descripcionUI = descripcionUI;
            copia.icono = icono;
            return copia;
        }
    }
}
