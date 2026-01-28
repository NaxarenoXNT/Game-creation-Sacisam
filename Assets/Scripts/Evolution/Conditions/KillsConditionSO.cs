using UnityEngine;
using Flags;

namespace Evolution.Conditions
{
    /// <summary>
    /// Condición: Matar X cantidad de enemigos de un tipo específico.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_Kills", menuName = "Evolutions/Conditions/Kills Tipo")]
    public class KillsConditionSO : EvolutionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("Tipo de entidad a eliminar")]
        public TipoEntidades tipoEntidad = TipoEntidades.None;
        
        [Tooltip("Cantidad de kills requeridos")]
        public int cantidad = 10;

        public override bool EsEscalable => true;

        public override bool Evaluar(EvolutionState state)
        {
            string key = tipoEntidad.ToString();
            state.killsPorTipo.TryGetValue(key, out int kills);
            return kills >= cantidad;
        }

        public override float GetProgreso(EvolutionState state)
        {
            string key = tipoEntidad.ToString();
            state.killsPorTipo.TryGetValue(key, out int kills);
            return cantidad > 0 ? Mathf.Clamp01((float)kills / cantidad) : 1f;
        }

        public override string GetDescripcionAuto()
        {
            return $"Elimina {cantidad} {tipoEntidad}";
        }

        public override EvolutionConditionSO CrearCopiaEscalada(float multiplicador)
        {
            var copia = CreateInstance<KillsConditionSO>();
            copia.tipoEntidad = tipoEntidad;
            copia.cantidad = Mathf.RoundToInt(cantidad * multiplicador);
            copia.descripcionUI = descripcionUI;
            copia.icono = icono;
            return copia;
        }
    }
}
