using System;
using System.Collections.Generic;
using System.Linq;

namespace Evolution
{
    /// <summary>
    /// Selecciona una oferta de evoluciones/traits ponderada por rareza/peso.
    /// Implementación placeholder: ajustar a tu regla final.
    /// </summary>
    public class EvolutionRoller
    {
        private readonly Random _rng;

        public EvolutionRoller(int seed)
        {
            _rng = new Random(seed);
        }

        public List<T> RolarOferta<T>(IEnumerable<T> candidatos, int cantidad) where T : IEvolutionOption
        {
            var lista = candidatos.ToList();
            if (lista.Count <= cantidad)
                return new List<T>(lista);

            // Ponderar por peso
            float pesoTotal = lista.Sum(o => o.PesoOferta);
            var resultado = new List<T>();
            var pool = new List<T>(lista);

            while (resultado.Count < cantidad && pool.Count > 0)
            {
                float roll = (float)(_rng.NextDouble() * pesoTotal);
                float acumulado = 0f;
                T elegido = pool[0];
                foreach (var opt in pool)
                {
                    acumulado += opt.PesoOferta;
                    if (roll <= acumulado)
                    {
                        elegido = opt;
                        break;
                    }
                }
                resultado.Add(elegido);
                pesoTotal -= elegido.PesoOferta;
                pool.Remove(elegido);
            }
            return resultado;
        }
    }

    /// <summary>
    /// Contrato mínimo para opciones rolables (evolución/trait).
    /// </summary>
    public interface IEvolutionOption
    {
        string Id { get; }
        float PesoOferta { get; }
    }
}
