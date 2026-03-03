using UnityEngine;

namespace Subclases.Modulos
{
    /// <summary>
    /// ScriptableObject del módulo Paladín.
    /// Configura los parámetros de bonificación desde el inspector.
    ///
    /// Crear desde: Assets → Create → Clases/Modulos/Paladin
    /// </summary>
    [CreateAssetMenu(fileName = "PaladinModuloSO", menuName = "Clases/Modulos/Paladin")]
    public class PaladinModuloSO : ModuloClaseSO
    {
        [Header("Curación")]
        [Tooltip("Bonus porcentual sobre toda curación otorgada y recibida. 0.20 = +20%")]
        [Range(0f, 1f)]
        public float bonusCuracion = 0.20f;

        [Header("Daño vs No-Muertos")]
        [Tooltip("Bonus porcentual de daño físico y elemental contra entidades Undead. 0.20 = +20%")]
        [Range(0f, 1f)]
        public float bonusDanoUndead = 0.20f;

        private void OnValidate()
        {
            moduloId = "paladin";
        }

        public override IComportamientoDeClase Instanciar()
            => new PaladinModulo(bonusCuracion, bonusDanoUndead);
    }
}
