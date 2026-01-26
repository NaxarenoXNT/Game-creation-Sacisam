using Padres;
using Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Subclases
{
    /// <summary>
    /// Orco: Enemigo resistente con stats balanceados. Ataca objetivos aleatorios.
    /// </summary>
    public class Orcos : Enemigos
    {
        // Escalado específico del Orco (stats medios-altos)
        private static readonly EscaladoEnemigo EscaladoOrco = new EscaladoEnemigo(
            vida: 150,
            ataque: 15,
            defensa: 8f,
            velocidad: 1
        );
        
        public Orcos(EnemigoData datos)
            : base(
                datos.nombreEnemigo,
                datos.vidaBase,
                datos.ataqueBase,
                datos.defensaBase,
                datos.nivelBase,  // Usar nivelBase del ScriptableObject
                datos.velocidadBase,
                (int)datos.xpOtorgada,
                datos.atributos,
                datos.tipoEntidad,
                datos.estiloCombate,
                EscaladoOrco      // Pasar escalado específico
            )
        {
        }

        public override IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores)
        {
            // Orcos atacan aleatoriamente
            var jugadoresVivos = jugadores.Where(j => j.EstaVivo()).ToList();
            if (jugadoresVivos.Count == 0) return null;

            int indice = UnityEngine.Random.Range(0, jugadoresVivos.Count);
            return jugadoresVivos[indice];
        }

        public override int CalcularDanoContra(IEntidadCombate objetivo)
        {
            // Orcos hacen dano estandar
            return PuntosDeAtaque_Entidad;
        }
    }
}