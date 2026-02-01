using UnityEngine;
using Flags;

namespace Habilidades
{
    /// <summary>
    /// Representa un costo de recurso para una habilidad.
    /// Una habilidad puede tener múltiples costos (ej: 10 Mana + 5 Fe).
    /// </summary>
    [System.Serializable]
    public class CostoRecurso
    {
        [Tooltip("Tipo de recurso a consumir")]
        public TipoRecurso tipo = TipoRecurso.Ninguno;
        
        [Tooltip("Cantidad a consumir (puede ser porcentaje si usaPorcentaje = true)")]
        [Min(0)]
        public float cantidad = 0;
        
        [Tooltip("Si es true, la cantidad es un porcentaje del máximo del recurso")]
        public bool usaPorcentaje = false;

        public CostoRecurso() { }

        public CostoRecurso(TipoRecurso tipo, float cantidad, bool usaPorcentaje = false)
        {
            this.tipo = tipo;
            this.cantidad = cantidad;
            this.usaPorcentaje = usaPorcentaje;
        }

        /// <summary>
        /// Calcula el costo real basado en el máximo del recurso (si usa porcentaje).
        /// </summary>
        public float CalcularCostoReal(float recursoMaximo)
        {
            if (usaPorcentaje)
                return recursoMaximo * (cantidad / 100f);
            return cantidad;
        }

        /// <summary>
        /// Verifica si este costo es significativo (tipo != Ninguno y cantidad > 0).
        /// </summary>
        public bool EsSignificativo()
        {
            return tipo != TipoRecurso.Ninguno && cantidad > 0;
        }

        public override string ToString()
        {
            if (!EsSignificativo()) return "Sin costo";
            string porcentaje = usaPorcentaje ? "%" : "";
            return $"{cantidad}{porcentaje} {tipo}";
        }
    }
}
