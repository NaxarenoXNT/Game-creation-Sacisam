using Padres;
using Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Subclases
{
    /// <summary>
    /// Goblin: Enemigo débil pero rápido. Ataca objetivos aleatorios.
    /// </summary>
    public class Goblin : Enemigos
    {
        // Escalado específico del Goblin (stats bajos)
        private static readonly EscaladoEnemigo EscaladoGoblin = new EscaladoEnemigo(
            vida: 50,
            ataque: 8,
            defensa: 3f,
            velocidad: 3
        );
        
        public Goblin(EnemigoData datos)
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
                EscaladoGoblin    // Pasar escalado específico
            )
        {
        }

        public override IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores)
        {
            // Goblin ataca aleatoriamente
            var jugadoresVivos = jugadores.Where(j => j.EstaVivo()).ToList();
            if (jugadoresVivos.Count == 0) return null;

            int indice = UnityEngine.Random.Range(0, jugadoresVivos.Count);
            return jugadoresVivos[indice];
        }

        public override int CalcularDanoContra(IEntidadCombate objetivo)
        {
            // Goblins hacen menos dano pero atacan rapido
            return (int)(PuntosDeAtaque_Entidad * 0.8f);
        }
    }
}