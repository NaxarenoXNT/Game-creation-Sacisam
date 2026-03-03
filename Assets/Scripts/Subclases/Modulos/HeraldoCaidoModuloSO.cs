using UnityEngine;

namespace Subclases.Modulos
{
    /// <summary>
    /// ScriptableObject del módulo Heraldo Caído.
    ///
    /// Crear desde: Assets → Create → Clases/Modulos/Heraldo Caido
    /// </summary>
    [CreateAssetMenu(fileName = "HeraldoCaidoModuloSO", menuName = "Clases/Modulos/Heraldo Caido")]
    public class HeraldoCaidoModuloSO : ModuloClaseSO
    {
        private void OnValidate()
        {
            moduloId = "heraldo_caido";
        }

        public override IComportamientoDeClase Instanciar()
            => new HeraldoCaidoModulo();
    }
}
