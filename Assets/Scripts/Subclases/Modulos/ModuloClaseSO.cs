using UnityEngine;

namespace Subclases.Modulos
{
    /// <summary>
    /// ScriptableObject abstracto que actúa como factory de IComportamientoDeClase.
    ///
    /// Cada evolución de clase tiene su propio SO concreto que:
    /// 1. Permite configurar parámetros (bonos, referencias) desde el inspector.
    /// 2. Genera la instancia C# concreta al llamar Instanciar().
    ///
    /// Uso:
    ///   var modulo = moduloSO.Instanciar();
    ///   jugador.AgregarModulo(modulo);
    /// </summary>
    public abstract class ModuloClaseSO : ScriptableObject
    {
        [Tooltip("ID único del módulo. Debe coincidir con IComportamientoDeClase.ModuloId.")]
        public string moduloId;

        /// <summary>
        /// Crea la instancia de comportamiento inicializada con los datos del SO.
        /// </summary>
        public abstract IComportamientoDeClase Instanciar();
    }
}
